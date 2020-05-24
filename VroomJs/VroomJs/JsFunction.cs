using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace VroomJs
{
    public sealed class JsFunction : JsObjectBase
    {
        private readonly IntPtr _thisPtr;

        public JsFunction(JsContext context, IntPtr objectHandle, IntPtr thisPtr)
            :base(context, objectHandle)
        {
            _thisPtr = thisPtr;
        }

        public object Invoke(object[] args)
        {
            CheckDisposed();

            var a = JsValue.Null; // Null value unless we're given args.
            if (args != null)
                a = Convert.ToJsValue(args);

            var v = NativeApi.jscontext_invoke(Context.Handle, Handle, _thisPtr, a);
            var res = Convert.FromJsValue(v);
            NativeApi.jsvalue_dispose(v);
            NativeApi.jsvalue_dispose(a);

            var e = res as JsException;
            if (e != null)
                throw e;
            return res;
        }

        public object MakeDelegate(Type type, object[] args)
        {
            if (type.BaseType != typeof(MulticastDelegate))
            {
                throw new ApplicationException("Not a delegate.");
            }

            MethodInfo invoke = type.GetMethod("Invoke");
            if (invoke == null)
            {
                throw new ApplicationException("Not a delegate.");
            }

            ParameterInfo[] invokeParams = invoke.GetParameters();
            Type returnType = invoke.ReturnType;

            List<ParameterExpression> parameters = new List<ParameterExpression>();
            List<Expression> arrayInitExpressions = new List<Expression>();

            for (int i = 0; i < invokeParams.Length; i++)
            {
                ParameterExpression param = Expression.Parameter(invokeParams[i].ParameterType, invokeParams[i].Name);
                parameters.Add(param);
                arrayInitExpressions.Add(Expression.Convert(param, typeof(object)));
            }

            Expression array = Expression.NewArrayInit(typeof(object), arrayInitExpressions);

            Expression me = Expression.Constant(this);
            MethodInfo myInvoke = GetType().GetMethod("Invoke");
            Expression callExpression = Expression.Call(me, myInvoke, array);

            if (returnType != typeof(void))
            {
                callExpression = Expression.Convert(callExpression, returnType);
            }

            return Expression.Lambda(type, callExpression, parameters).Compile();
        }

        #region IDisposable implementation
 
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_thisPtr != IntPtr.Zero)
            {
                Context.Engine.DisposeObject(_thisPtr);
            }
        }

        #endregion
     }
}
