using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    internal static class JsonExtensions
    {
        public static void SetFileSource(this JToken json, string filePath)
        {
            json.AddAnnotation(new JsonFileSourceAnnotation(filePath));
        }
        public static void SetFileSource(this JContainer json, string filePath)
        {
            foreach (JToken token in json.DescendantsAndSelf())
                SetFileSource(token, filePath);
        }

        public static SourceLocation GetSourceInfo(this JToken token)
        {
            string filePath = (token.Annotation<JsonFileSourceAnnotation>() ?? token.Root.Annotation<JsonFileSourceAnnotation>())?.FilePath;

            IJsonLineInfo lineInfo = token;

            bool hasLineInfo = lineInfo.HasLineInfo();
            int lineNumber = lineInfo.LineNumber;
            int linePosition = lineInfo.LinePosition;

            if (hasLineInfo)
            {
                switch (token)
                {
                    case JValue value:
                        linePosition = value.GetCorrectLinePosition();
                        break;

                    case JProperty property:
                        linePosition = property.GetCorrectLinePosition();
                        break;
                }
            }

            return new SourceLocation(filePath, lineNumber, linePosition);
        }

        public static T Merge<T>(this T source, T content) where T : JContainer
        {
            if (content == null)
                return source;

            // We don't want our content to overwrite the source, if a property has been explicitly set.
            // We could inverse source and content and do 'content.Merge(source)'.
            // That would however modify the root document.
            // For example in a template use case, the action definition root will now be the one of the global template.
            //object contentCopy = content.DeepClone();
            //source.Merge(contentCopy);

            JsonMerger<JContainer>.Merge(source, content);

            return source;
        }

        // The line positions are somewhat weird and unexpected
        // Not sure if this is a bug, but we have to adjust the position to get the actual start of the value
        private static int GetCorrectLinePosition(this JValue value)
        {
            IJsonLineInfo lineInfo = value;
            StringBuilder sb = new StringBuilder();
            using (TextWriter textWriter = new StringWriter(sb))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                {
                    value.WriteTo(jsonWriter);
                    int valueEnd = lineInfo.LinePosition + 1;
                    int result = valueEnd - sb.Length;

                    // And while we're at it anyways, we can skip ahead the " just for convenience
                    if (value.Type == JTokenType.String)
                        result++;

                    return result;
                }
            }
        }
        private static int GetCorrectLinePosition(this JProperty property)
        {
            IJsonLineInfo lineInfo = property;
            int result = lineInfo.LinePosition - 1 - property.Name.Length;
            return result;
        }
    }

    internal abstract class JsonMerger<T> where T : JContainer
    {
        public static void Merge(JContainer container, object content, JsonMergeSettings settings = null)
        {
            switch (container)
            {
                case JArray array:
                    new JsonArrayMerger().MergeContent(array, content, settings);
                    break;

                case JConstructor ctor:
                    new JsonConstructorMerger().MergeContent(ctor, content, settings);
                    break;

                case JObject obj:
                    // This is the main reason we are not using Newtonsoft's Merge function
                    const bool replaceExistingProperties = false;
                    new JObjectMerger(replaceExistingProperties).MergeContent(obj, content, settings);
                    break;

                case JProperty property:
                    new JPropertyMerger().MergeContent(property, content, settings);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(container));
            }
        }

        public abstract void MergeContent(T container, object content, JsonMergeSettings settings);

        private static void MergeEnumerableContent(JContainer target, IEnumerable content, JsonMergeSettings settings)
        {
            switch (settings?.MergeArrayHandling ?? MergeArrayHandling.Concat)
            {
                case MergeArrayHandling.Concat:
                    foreach (JToken item in content) 
                        target.Add(item);

                    break;

                case MergeArrayHandling.Union:
                    HashSet<JToken> items = new HashSet<JToken>(target, JToken.EqualityComparer);
                    foreach (JToken item in content)
                    {
                        if (items.Add(item)) 
                            target.Add(item);
                    }
                    break;

                case MergeArrayHandling.Replace:
                    if (Equals(target, content))
                        break;

                    ((ICollection<JToken>)target).Clear();
                    foreach (JToken item in content) 
                        target.Add(item);

                    break;

                case MergeArrayHandling.Merge:
                    int i = 0;
                    foreach (object targetItem in content)
                    {
                        if (i < target.Count)
                        {
                            JToken sourceItem = target[i];
                            if (sourceItem is JContainer existingContainer)
                            {
                                Merge(existingContainer, targetItem, settings);
                            }
                            else
                            {
                                if (targetItem != null)
                                {
                                    JToken contentValue = CreateFromContent(targetItem);
                                    if (contentValue.Type != JTokenType.Null)
                                    {
                                        target[i] = contentValue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            target.Add(targetItem);
                        }

                        i++;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(settings), "Unexpected merge array handling when merging JSON.");
            }
        }

        private static JToken CreateFromContent(object content) => content as JToken ?? new JValue(content);

        private sealed class JsonArrayMerger : JsonMerger<JArray>
        {
            public override void MergeContent(JArray container, object content, JsonMergeSettings settings)
            {
                IEnumerable enumerableContent = IsMultiContent(content) || content is JArray ? (IEnumerable)content : null;
                
                if (enumerableContent == null)
                    return;

                MergeEnumerableContent(container, enumerableContent, settings);
            }

            private static bool IsMultiContent(object content) => content is IEnumerable and not string and not JToken and not byte[];
        }

        private sealed class JsonConstructorMerger : JsonMerger<JConstructor>
        {
            public override void MergeContent(JConstructor container, object content, JsonMergeSettings settings)
            {
                if (content is not JConstructor contentCtor)
                    return;

                if (contentCtor.Name != null) 
                    container.Name = contentCtor.Name;

                MergeEnumerableContent(container, contentCtor, settings);
            }
        }

        private sealed class JObjectMerger : JsonMerger<JObject>
        {
            private readonly bool _replaceExistingProperties;

            public JObjectMerger(bool replaceExistingProperties = true)
            {
                _replaceExistingProperties = replaceExistingProperties;
            }

            public override void MergeContent(JObject container, object content, JsonMergeSettings settings)
            {
                if (content is not JObject contentObj)
                    return;

                foreach (KeyValuePair<string, JToken> contentItem in contentObj)
                {
                    JProperty existingProperty = container.Property(contentItem.Key, settings?.PropertyNameComparison ?? StringComparison.Ordinal);

                    if (existingProperty == null)
                    {
                        container.Add(contentItem.Key, contentItem.Value);
                    }
                    else if (contentItem.Value != null)
                    {
                        if (existingProperty.Value is not JContainer existingContainer || existingContainer.Type != contentItem.Value.Type)
                        {
                            bool isNull = IsNull(contentItem.Value);
                            if (isNull && settings?.MergeNullValueHandling == MergeNullValueHandling.Merge
                             || !isNull && _replaceExistingProperties)
                            {
                                existingProperty.Value = contentItem.Value;
                            }
                        }
                        else
                        {
                            Merge(existingContainer, contentItem.Value, settings);
                        }
                    }
                }
            }

            private static bool IsNull(JToken token) => token.Type == JTokenType.Null || token is JValue { Value: null };
        }

        private sealed class JPropertyMerger : JsonMerger<JProperty>
        {
            public override void MergeContent(JProperty container, object content, JsonMergeSettings settings)
            {
                JToken value = (content as JProperty)?.Value;

                if (value != null && value.Type != JTokenType.Null)
                {
                    container.Value = value;
                }
            }
        }
    }
}