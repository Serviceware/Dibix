namespace Dibix
{
    internal class MultiMapperExtension
    {
        private readonly MultiMapper _mapper;

        public MultiMapperExtension(MultiMapper mapper) => _mapper = mapper;

        public TReturn MapRowWithProjection<TReturn>(object[] args) where TReturn : new() => _mapper.MapRow<TReturn>(useProjection: true, args);

        public TReturn MapRowWithoutProjection<TReturn>(object[] args) where TReturn : new() => _mapper.MapRow<TReturn>(useProjection: false, args);
    }
}