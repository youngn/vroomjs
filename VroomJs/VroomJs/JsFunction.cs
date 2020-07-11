using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VroomJs.Interop;

namespace VroomJs
{
    public class JsFunction : JsObject
    {
        internal JsFunction(JsContext context, IntPtr objectHandle)
            :base(context, objectHandle)
        {
        }

        public object Invoke(object receiver, params object[] args)
        {
            CheckDisposed();

            var jsargs = (args ?? Array.Empty<object>()).Select(a => (jsvalue)JsValue.ForValue(a, Context)).ToArray();
            var recv = JsValue.ForValue(receiver, Context);

            var v = (JsValue)NativeApi.jsfunction_invoke(Context.Handle, Handle, recv, jsargs.Length, jsargs);
            var res = v.Extract(Context);

            var e = res as JsException;
            if (e != null)
                throw e;

            return res;
        }

        // todo: what is this for?
        internal object MakeDelegate(Type type)
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
     }
}
