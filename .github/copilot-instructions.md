# Copilot Instructions for Dibix Repository

## Code Quality Standards

### Analyzer Rules Compliance
- Follow all StyleCop analyzer rules as configured in .editorconfig
- Respect all formatting and style settings defined in .editorconfig
- Use existing project indentation and formatting patterns

### When Suggesting Code Changes
1. Always follow the .editorconfig configuration
2. Follow existing formatting patterns in the file
3. Respect the project's naming conventions
4. Ensure compliance with all enabled analyzer rules
5. Use the same indentation style as surrounding code

### Project-Specific Guidelines
- This is a .NET project using StyleCop analyzers
- All analyzer warnings are treated as errors in build
- Code suggestions should compile without warnings
- Follow existing patterns for dependency injection and middleware setup

## MCP Integration Notes
- MCP server is integrated into existing Dibix.Http.Host
- Only enabled in Development environment
- Uses HTTP/SSE transport, not stdio
- Focus on clean, maintainable code that fits the existing architecture