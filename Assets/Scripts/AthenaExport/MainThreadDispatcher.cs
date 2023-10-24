using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();
    private static readonly object queueLock = new object();

    public void Update()
    {
        while (true)
        {
            Action action = null;

            // Thread-safe dequeue
            lock (queueLock)
            {
                if (ExecutionQueue.Count == 0)
                    break;

                action = ExecutionQueue.Dequeue();
            }

            action?.Invoke();
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return; // Avoid null actions

        lock (queueLock)
        {
            ExecutionQueue.Enqueue(action);
        }
    }
}
