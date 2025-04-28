using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;

namespace NexusMods.App.Generators.Diagnostics;

[PublicAPI]
public sealed class CodeWriter
{
    private const char NewLine = '\n';

    private readonly ImmutableHashSet<string> _existingMethods;
    private readonly StringBuilder _stringBuilder = new();
    private int _depth;

    public CodeWriter() : this(ImmutableHashSet<string>.Empty) { }
    public CodeWriter(ImmutableHashSet<string> existingMethods)
    {
        _existingMethods = existingMethods;
    }

    public CodeBlock AddBlock() => new(this);
    public RegionBlock AddRegionBlock(string regionName) => new(this, regionName);

    private void Indent()
    {
        if (_stringBuilder.Length == 0) return;
        if (_stringBuilder[_stringBuilder.Length - 1] != NewLine) return;
        _stringBuilder.Append(new string('\t', _depth));
    }

    public CodeWriter AddSingleLineMethod(
        [LanguageInjection("csharp")] string extendedSignature,
        [LanguageInjection("csharp")] string body)
    {
        if (_existingMethods.Contains(extendedSignature)) return this;
        return Append(extendedSignature).Append(" ").AppendLine(body).AppendLine();
    }

    public MethodWriter AddMethod()
    {
        return new MethodWriter(this);
    }

    public CodeWriter AppendUnindented([LanguageInjection("csharp")] string text)
    {
        _stringBuilder.Append(text);
        return this;
    }

    public CodeWriter AppendLineUnindented([LanguageInjection("csharp")] string line)
    {
        return AppendUnindented(line).AppendLine();
    }

    public CodeWriter Append([LanguageInjection("csharp")] string text)
    {
        Indent();
        _stringBuilder.Append(text);
        return this;
    }

    public CodeWriter AppendLine([LanguageInjection("csharp")] string line)
    {
        return Append(line).AppendLine();
    }

    public CodeWriter AppendLine()
    {
        if (_stringBuilder.Length < 2)
        {
            _stringBuilder.Append(NewLine);
            return this;
        }

        if (_stringBuilder[_stringBuilder.Length - 1] == NewLine &&
            _stringBuilder[_stringBuilder.Length - 2] == NewLine)
        {
            return this;
        }

        _stringBuilder.Append(NewLine);
        return this;
    }

    public override string ToString() => _stringBuilder.ToString();

    public readonly struct CodeBlock : IDisposable
    {
        private readonly CodeWriter _cw;
        public CodeBlock(CodeWriter cw)
        {
            _cw = cw;
            _cw.AppendLine("{");
            _cw._depth++;
        }

        public void Dispose()
        {
            _cw._depth--;
            _cw.AppendLine("}");
            _cw.AppendLine();
        }
    }

    public readonly struct RegionBlock : IDisposable
    {
        private readonly CodeWriter _cw;
        private readonly string _regionName;

        public RegionBlock(CodeWriter cw, string regionName)
        {
            _cw = cw;
            _regionName = regionName;

            _cw.AppendLine();
            _cw.AppendLineUnindented($"#region {regionName}");
            _cw.AppendLine();
        }

        public void Dispose()
        {
            _cw.AppendLine();
            _cw.AppendLineUnindented($"#endregion {_regionName}");
            _cw.AppendLine();
        }
    }

    public struct MethodWriter : IDisposable
    {
        private readonly CodeWriter _cw;
        private readonly List<string> _linesToAdd = new();
        private MethodBlock? _currentBlock = null;

        public MethodWriter(CodeWriter cw)
        {
            _cw = cw;
        }

        public readonly void AddDocumentation([LanguageInjection("csharp")] string docs)
        {
            _linesToAdd.Add(docs);
        }

        public readonly void AddAttribute([LanguageInjection("csharp")] string attribute)
        {
            _linesToAdd.Add(attribute);
        }

        public readonly void AddInheritInlineAttributes()
        {
            _linesToAdd.Add(Constants.InheritDocumentation);
            _linesToAdd.Add(Constants.InlineAttribute);
        }

        private readonly void AddLines()
        {
            foreach (var lineToAdd in _linesToAdd)
            {
                _cw.AppendLine(lineToAdd);
            }
        }

        public readonly void AppendLine([LanguageInjection("csharp")] string line)
        {
            Debug.Assert(_currentBlock.HasValue);
            if (!_currentBlock.HasValue) throw new InvalidOperationException();

            if (!_currentBlock.Value.Active) return;
            _cw.Append(line).AppendLine();
        }

        public readonly void AddSingleLineMethod(
            [LanguageInjection("csharp")] string extendedSignature,
            [LanguageInjection("csharp")] string body)
        {
            if (_cw._existingMethods.Contains(extendedSignature)) return;
            AddLines();

            _cw.Append(extendedSignature).Append(" ").AppendLine(body).AppendLine();
        }

        public MethodBlock AddMethodBlock([LanguageInjection("csharp")] string extendedSignature)
        {
            var active = !_cw._existingMethods.Contains(extendedSignature);
            if (!active)
            {
                _currentBlock = new MethodBlock(active, _cw);
                return _currentBlock.Value;
            }

            AddLines();
            _cw.AppendLine(extendedSignature);

            _currentBlock = new MethodBlock(active, _cw);
            return _currentBlock.Value;
        }

        public readonly void Dispose() { }
    }

    public readonly struct MethodBlock : IDisposable
    {
        public readonly bool Active;
        private readonly CodeWriter _cw;

        public MethodBlock(bool active, CodeWriter cw)
        {
            Active = active;
            _cw = cw;

            if (!active) return;
            _cw.AppendLine("{");
            _cw._depth++;
        }

        public void Dispose()
        {
            if (!Active) return;
            _cw._depth--;
            _cw.AppendLine("}");
            _cw.AppendLine();
        }
    }
}
