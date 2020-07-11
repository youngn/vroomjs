namespace VroomJs
{
    public class JsUndefined
    {
        public static readonly JsUndefined Value = new JsUndefined();

        private JsUndefined()
        {
        }

        public override string ToString()
        {
            return "undefined";
        }
    }
}
