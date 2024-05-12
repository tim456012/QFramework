/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 * 
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using UnityEngine;

namespace QFramework
{
    public enum ActionStatus
    {
        NotStart,
        Started,
        Finished,
    }

    public interface IActionController
    {
        ulong ActionID { get; set; }

        IAction Action { get; set; }

        bool Paused { get; set; }
        void Reset();
        void Deinit();
    }

    public interface IAction<TStatus>
    {
        ulong ActionID { get; set; }
        TStatus Status { get; set; }
        void OnStart();
        void OnExecute(float dt);
        void OnFinish();

        bool Deinited { get; set; }

        bool Paused { get; set; }
        void Reset();
        void Deinit();
    }


    public interface IAction : IAction<ActionStatus>
    {
    }

    public abstract class AbstractAction<T> : IAction where T : AbstractAction<T>,new()
    {
        protected AbstractAction(){}
        
        private static readonly SimpleObjectPool<T> mPool =
            new SimpleObjectPool<T>(() => new T(), null, 10);

        public static T Allocate()
        {
            var retNode = mPool.Allocate();
            retNode.ActionID = ActionKit.ID_GENERATOR++;
            retNode.Deinited = false;
            retNode.Reset();
            return retNode;
        }

        public ulong ActionID { get; set; }
        public ActionStatus Status { get; set; }
        
        public virtual void OnStart() {}

        public virtual void OnExecute(float dt) {}

        public virtual void OnFinish() { }

        protected virtual void OnReset(){}
        
        protected virtual void OnDeinit(){}

        public void Reset()
        {
            Status = ActionStatus.NotStart;
            Paused = false;
            OnReset();
        }

        public bool Paused { get; set; }

        public void Deinit()
        {
            if (!Deinited)
            {
                Deinited = true;
                OnDeinit();
                ActionQueue.AddCallback(new ActionQueueRecycleCallback<T>(mPool,this as T));
            }
        }
        
        public bool Deinited { get; set; }
    }

    public struct ActionController : IActionController
    {
        public ulong ActionID { get; set; }
        public IAction Action { get; set; }

        public bool Paused
        {
            get => Action.Paused;
            set => Action.Paused = value;
        }

        public void Reset()
        {
            if (Action.ActionID == ActionID)
            {
                Action.Reset();
            }
        }

        public void Deinit()
        {
            if (Action.ActionID == ActionID)
            {
                Action.Deinit();
            }
        }
    }


    public static class IActionExtensions
    {
        public static IActionController Start(this IAction self, MonoBehaviour monoBehaviour,
            Action<IActionController> onFinish = null)
        {
            monoBehaviour.ExecuteByUpdate(self, onFinish);

            return new ActionController()
            {
                Action = self,
                ActionID = self.ActionID,
            };
        }

        public static IActionController Start(this IAction self, MonoBehaviour monoBehaviour,
            Action onFinish)
        {
            monoBehaviour.ExecuteByUpdate(self, _ => onFinish());

            return new ActionController()
            {
                Action = self,
                ActionID = self.ActionID,
            };
        }
        
        public static IActionController StartCurrentScene(this IAction self, Action<IActionController> onFinish = null)
        {
            return self.Start(ActionKitCurrentScene.SceneComponent, onFinish);
        }
        
        public static IActionController StartCurrentScene(this IAction self, Action onFinish)
        {
            return self.Start(ActionKitCurrentScene.SceneComponent, onFinish);
        }

        public static IActionController StartGlobal(this IAction self, Action<IActionController> onFinish = null)
        {
            return self.Start(ActionKitMonoBehaviourEvents.Instance, onFinish);
        }
        
        public static IActionController StartGlobal(this IAction self, Action onFinish)
        {
            return self.Start(ActionKitMonoBehaviourEvents.Instance, onFinish);
        }


        public static void Pause(this IActionController self)
        {
            if (self.ActionID == self.Action.ActionID)
            {
                self.Action.Paused = true;
            }
        }

        public static void Resume(this IActionController self)
        {
            if (self.ActionID == self.Action.ActionID)
            {
                self.Action.Paused = false;
            }
        }

        public static void Finish(this IAction self)
        {
            self.Status = ActionStatus.Finished;
        }

        public static bool Execute(this IAction self, float dt)
        {
            if (self.Status == ActionStatus.NotStart)
            {
                self.OnStart();

                if (self.Status == ActionStatus.Finished)
                {
                    self.OnFinish();
                    return true;
                }

                self.Status = ActionStatus.Started;
            }
            else if (self.Status == ActionStatus.Started)
            {
                if (self.Paused) return false;

                self.OnExecute(dt);

                if (self.Status == ActionStatus.Finished)
                {
                    self.OnFinish();
                    return true;
                }
            }
            else if (self.Status == ActionStatus.Finished)
            {
                self.OnFinish();
                return true;
            }

            return false;
        }
    }
}