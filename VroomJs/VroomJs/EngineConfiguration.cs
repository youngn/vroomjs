namespace VroomJs
{
    public class EngineConfiguration
    {
        public class MemoryConfiguration
        {
            public int MaxYoungSpace { get; set; } = -1;

            public int MaxOldSpace { get; set; } = -1;
        }


        public MemoryConfiguration Memory { get; set; } = new MemoryConfiguration();

        internal void Apply(JsEngine engine)
        {
        }
    }
}
