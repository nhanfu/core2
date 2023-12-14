using Bridge.Html5;
using Core.Enums;
using System;
using System.Threading.Tasks;

namespace Core.Extensions
{
    public static class EventExt
    {
        public static float Top(this Event e) => (float)e["clientY"];
        public static float Left(this Event e) => (float)e["clientX"];
        public static int KeyCode(this Event e) => (int?)e["keyCode"] ?? -1;
        public static KeyCodeEnum? KeyCodeEnum(this Event e)
        {
            if (e["keyCode"] == null)
            {
                return null;
            }
            var parsed = Enum.TryParse(e["keyCode"].ToString().ToUpper(), out KeyCodeEnum res);
            return parsed ? (KeyCodeEnum?)res : null;
        }
        public static bool ShiftKey(this Event e) => (bool)e["shiftKey"];
        /// <summary>
        /// Detect if the user press Ctrl or Command key while the event occurs
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool CtrlOrMetaKey(this Event e) => (bool)e["ctrlKey"] || (bool)e["metaKey"];
        public static bool AltKey(this Event e) => (bool)e["altKey"];
        public static bool GetChecked(this Event e) => e.Target.Cast<HTMLInputElement>().Checked;
        public static string GetInputText(this Event e) => e.Target.Cast<HTMLInputElement>().Value;

        public static IPromise ToPromise<T>(this Task<T> task)
        {
            if (task == null) return null;
            /*@
            return new Promise((resolve, reject) => {
            var $step = 0,
                $task1, 
                $taskResult1, 
                $jumpFromFinally, 
                $returnValue, 
                t, 
                $async_e, 
                $asyncBody = Bridge.fn.bind(this, function () {
                    try {
                        for (;;) {
                            $step = System.Array.min([0,1], $step);
                            switch ($step) {
                                case 0: {
                                    if (task == null) {
                                        resolve(null);
                                        return;
                                    }
                                    $task1 = task;
                                    $step = 1;
                                    if ($task1.isCompleted()) {
                                        continue;
                                    }
                                    $task1.continue($asyncBody);
                                    return;
                                }
                                case 1: {
                                    $taskResult1 = $task1.getAwaitedResult();
                                    t = $taskResult1;
                                    resolve(t);
                                    return;
                                }
                                default: {
                                    resolve(null);
                                    return;
                                }
                            }
                        }
                    } catch($async_e1) {
                        $async_e = System.Exception.create($async_e1);
                        reject($async_e);
                    }
                }, arguments);

            $asyncBody();
            });
            */
            return null;
        }

        public static IPromise ToPromiseNoResult(Task task)
        {
            if (task == null) return null;
            /*@
            return new Promise((resolve, reject) => {
            var $step = 0,
                $task1, 
                $taskResult1, 
                $jumpFromFinally, 
                $returnValue, 
                t, 
                $async_e, 
                $asyncBody = Bridge.fn.bind(this, function () {
                    try {
                        for (;;) {
                            $step = System.Array.min([0,1], $step);
                            switch ($step) {
                                case 0: {
                                    if (task == null) {
                                        resolve(null);
                                        return;
                                    }
                                    $task1 = task;
                                    $step = 1;
                                    if ($task1.isCompleted()) {
                                        continue;
                                    }
                                    $task1.continue($asyncBody);
                                    return;
                                }
                                case 1: {
                                    $taskResult1 = $task1.getAwaitedResult();
                                    t = $taskResult1;
                                    resolve(t);
                                    return;
                                }
                                default: {
                                    resolve(null);
                                    return;
                                }
                            }
                        }
                    } catch($async_e1) {
                        $async_e = System.Exception.create($async_e1);
                        reject($async_e);
                    }
                }, arguments);

            $asyncBody();
            });
            */
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public static IPromise Done<T>(this Task<T> task, Action<T> handler = null)
        {
            var promise = task.ToPromise();
            /*@
            promise.then(handler);
             */
            return promise;
        }

        public static IPromise Catch(this IPromise task, Action<Exception> handler = null)
        {
            /*@
            task.catch(handler);
             */
            return task;
        }

        public static IPromise Finally(this IPromise task, Action handler = null)
        {
            /*@
            task.finally(handler);
             */
            return task;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public static IPromise Done(this Task task, Action handler = null, Action<Exception> errorHandler = null)
        {
            if (task is null) return null;
            var promise = ToPromiseNoResult(task);
            /*@
            promise.then(handler);
            if (errorHandler != null) promise.catch(errorHandler);
             */
            return promise;
        }
    }
}
