using System;
using System.Collections.Generic;

namespace VroomJs
{
    public class EngineConfiguration
    {
        public class ClrTemplateConfiguration
        {
            public bool EnableObjects { get; set; }
            public Predicate<object> ObjectFilter { get; set; }
            public bool UseNetToString { get; set; }
            public bool EnableTypes { get; set; }
            public Predicate<Type> TypeFilter { get; set; }
            public bool AllowInvokeTypeConstructor { get; set; }
            public bool EnableDelegates { get; set; }
            public Predicate<Delegate> DelegateFilter { get; set; }
        }

        public class MemoryConfiguration
        {
            public int MaxYoungSpace { get; set; } = -1;

            public int MaxOldSpace { get; set; } = -1;
        }

        private List<(HostObjectTemplate, Predicate<object>)> _customTemplates;

        public MemoryConfiguration Memory { get; set; } = new MemoryConfiguration();

        public ClrTemplateConfiguration ClrTemplates { get; set; } = new ClrTemplateConfiguration();

        /// <summary>
        /// Gets or sets the host-error filter.
        /// </summary>
        /// <remarks>
        /// The filter allows client code to interecept and modify a <see cref="HostErrorInfo"/>
        /// before it is raised as a JavaScript error. This may be used, for example, to customize
        /// the script error object based on the host exception type.
        /// </remarks>
        public HostErrorFilterDelegate HostErrorFilter { get; set; }

        public EngineConfiguration RegisterHostObjectTemplate(HostObjectTemplate template, Predicate<object> selector = null)
        {
            if (_customTemplates == null)
                _customTemplates = new List<(HostObjectTemplate, Predicate<object>)>();

            _customTemplates.Add((template, selector));

            return this;
        }

        internal void Apply(JsEngine engine)
        {
            // Order in which templates are registered is significant.
            // Templates with more specific selectors must be registered before
            // those with more general selectors.
            // Custom templates are registered before built-in CLR templates.

            if(_customTemplates != null)
            {
                foreach(var item in _customTemplates)
                {
                    engine.RegisterHostObjectTemplate(item.Item1, item.Item2);
                }
            }

            if(ClrTemplates.EnableDelegates)
            {
                var delegateFilter = ClrTemplates.DelegateFilter ?? (x => true);
                engine.RegisterHostObjectTemplate(new ClrDelegateTemplate(), obj => obj is Delegate d && delegateFilter(d));
            }

            // Both types and objects can have methods
            if(ClrTemplates.EnableTypes || ClrTemplates.EnableObjects)
            {
                engine.RegisterHostObjectTemplate(new ClrMethodTemplate(), obj => obj is WeakDelegate);
            }

            if(ClrTemplates.EnableTypes)
            {
                var typeFilter = ClrTemplates.TypeFilter ?? (x => true);
                engine.RegisterHostObjectTemplate(new ClrTypeTemplate(allowInvokeConstructor: ClrTemplates.AllowInvokeTypeConstructor),
                    obj => obj is Type t && typeFilter(t));
            }

            if(ClrTemplates.EnableObjects)
            {
                engine.RegisterHostObjectTemplate(new ClrObjectTemplate(useNetToString: ClrTemplates.UseNetToString), ClrTemplates.ObjectFilter);
            }

            if(HostErrorFilter != null)
            {
                engine.HostErrorFilter = HostErrorFilter;
            }
        }
    }
}
