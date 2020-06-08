using System.Collections.Generic;
using System.Text;

namespace VroomJs
{
    public class JsStackTrace
    {
        public class Frame
        {
            internal Frame(string resource, string function, int line, int column)
            {
                Line = line;
                Column = column;
                Resource = resource;
                Function = function;
            }

            public string Resource { get; }
            public string Function { get; }
            public int Line { get; }
            public int Column { get; }

            public override string ToString()
            {
                return !string.IsNullOrEmpty(Function)
                    ? $"{Function} ({Resource}:{Line}:{Column})"
                    : $"{Resource}:{Line}:{Column}";
            }
        }

        private readonly List<Frame> _frames;

        internal JsStackTrace(List<Frame> frames)
        {
            _frames = frames;
        }

        public IReadOnlyList<Frame> Frames => _frames;

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach(var frame in _frames)
            {
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append($"    at {frame}");
            }
            return sb.ToString();
        }
    }
}
