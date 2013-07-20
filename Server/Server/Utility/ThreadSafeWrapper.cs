﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Utility
{
    public class ThreadSafeWrapper<ObjectType>
    {
        private ObjectType m_object;
        private Fiber m_fiber;

        public ThreadSafeWrapper(ObjectType obj, Fiber fiber)
        {
            m_object = obj;
            m_fiber = fiber;
        }

        public Future<ReturnType> Access<ReturnType>(Func<ObjectType, ReturnType> accessor)
        {
            Future<ReturnType> future = new Future<ReturnType>();

            ReturnType synchronousResult = default(ReturnType);
            if (Monitor.TryEnter(m_object))
            {
                synchronousResult = accessor(m_object);
                Monitor.Exit(m_object);

                future.SetResult(synchronousResult);
            }
            else
            {
                m_fiber.Enqueue(() =>
                {
                    ReturnType result = default(ReturnType);
                    lock (m_object)
                    {
                        result = accessor(m_object);
                    }
                    future.SetResult(result);
                });
            }

            return future;
        }

        public void Access(Action<ObjectType> accessor)
        {
            if (Monitor.TryEnter(m_object))
            {
                accessor(m_object);
                Monitor.Exit(m_object);
            }
            else
            {
                m_fiber.Enqueue(() =>
                {
                    lock (m_object)
                    {
                        accessor(m_object);
                    }
                });
            }
        }
    }
}
