using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HhotateA.AvatarModifyTools.Core
{
    public class AnimatorControllerCreator
    {
        private AnimatorController asset;

        private AnimatorStateMachine stateMachine;
        private int layer = 0;

        AnimatorStateMachine GetStateMachine()
        {
            return asset.layers[layer].stateMachine;
        }
        
        public AnimatorControllerCreator(string name,string layerName = "")
        {
            asset = new AnimatorController()
            {
                name = name,
            };
            asset.AddLayer(
                new AnimatorControllerLayer()
                {
                    avatarMask = null,
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    defaultWeight = 1f,
                    iKPass =  false,
                    name = layerName,
                    stateMachine = new AnimatorStateMachine(),
                });
        }
        
        /// <summary>
        /// パラメーターを追加する
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public void AddParameter(string name,AnimatorControllerParameterType type)
        {
            asset.AddParameter(name,type);
        }

        public class MotionConditionPair
        {
            public Motion motion;
            public AnimatorCondition[] condition;
        }

        /// <summary>
        /// AnyStateからDefaultStateにいたる遷移を追加する
        /// </summary>
        /// <param name="m"></param>
        /// <param name="entryCondition"></param>
        /// <param name="exitCondition"></param>
        /// <returns></returns>
        public AnimatorState AnyStateToDefault(Motion motion,
            AnimatorCondition[] entryCondition,
            AnimatorCondition[] exitCondition = null,
            List<MotionConditionPair> motions = null)
        {
            List<AnimatorState> childStates = new List<AnimatorState>();
            if (motions == null) motions = new List<MotionConditionPair>();
            motions.InsertRange(0,new List<MotionConditionPair>()
            {
                new MotionConditionPair() {motion = motion, condition = exitCondition}
            });
            
            for (int i = 0; i < motions.Count; i++)
            {
                childStates.Add(GetStateMachine().AddState(motions[i].motion ? motions[i].motion.name : "State",RandomPosition()));
                {
                    childStates[i].motion = motions[i].motion;
                    childStates[i].writeDefaultValues = false;
                }
            }
            
            AnimatorState deststate = GetStateMachine().defaultState;
            for (int i = 0; i < motions.Count; i++)
            {
                if (i == motions.Count - 1) deststate = GetStateMachine().defaultState;
                else deststate = childStates[i + 1];
                
                if(motions[i].condition==null)
                {
                    var exit = childStates[i].AddTransition(deststate);
                    {
                        exit.canTransitionToSelf = false;
                        exit.duration = 0f;
                        exit.exitTime = 1f;
                        exit.hasExitTime = true;
                        exit.hasFixedDuration = true;
                        exit.conditions = null;
                    };
                }
                else
                {
                    var exit = childStates[i].AddTransition(deststate);
                    {
                        exit.canTransitionToSelf = false;
                        exit.duration = 0f;
                        exit.exitTime = 0f;
                        exit.hasExitTime = false;
                        exit.hasFixedDuration = true;
                        exit.conditions = motions[i].condition;
                    };
                }
            }
            
            var entry = GetStateMachine().AddAnyStateTransition(childStates[0]);
            {
                entry.canTransitionToSelf = false;
                entry.duration = 0f;
                entry.exitTime = 0f;
                entry.hasExitTime = false;
                entry.hasFixedDuration = true;
                entry.conditions = entryCondition;
            };

            return childStates[0];
        }
        
        /// <summary>
        /// DefaultStateを追加する．
        /// </summary>
        /// <param name="name"></param>
        /// <param name="m"></param>
        public void AddDefaultState(string name,Motion m = null)
        {
            AddState(name,m);
            SetDefaultState(name);
        }
        
        /// <summary>
        /// Stateを追加する．
        /// </summary>
        /// <param name="name"></param>
        /// <param name="motion"></param>
        public AnimatorState AddState(string name,Motion motion = null)
        {
            var s = GetStateMachine().AddState(name, RandomPosition());
            s.name = name;
            s.motion = motion;
            s.writeDefaultValues = false;
            return GetState("name");
        }
        
        /// <summary>
        /// DefaultStateを設定する
        /// </summary>
        /// <param name="name"></param>
        public void SetDefaultState(string name)
        {
            var d = GetState(name);
            if (d != null) GetStateMachine().defaultState = d;
        }

        /// <summary>
        /// fromStateからtoStateへのTransitionを作成する
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="condition"></param>
        /// <param name="hasExitTime"></param>
        /// <param name="exitTime"></param>
        /// <param name="duration"></param>
        public void AddTransition(
            string from, string to,
            AnimatorCondition[] condition,
            bool hasExitTime,
            float exitTime,
            float duration)
        {
            var f = GetState(from);
            var t = GetState(to);
            AnimatorStateTransition transition;
            if(from == "Any" && t != null)
            {
                transition = GetStateMachine().AddAnyStateTransition(t);

            }
            else if (to == "Exit" && f != null)
            {
                transition = f.AddExitTransition();
            }
            else if(t != null && f != null)
            {
                transition = f.AddTransition(t);
            }
            else
            {
                return;
            }

            transition.conditions = condition;
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
        }

        /// <summary>
        /// 名前からStateを探す
        /// </summary>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public AnimatorState GetState(string name, int layer = -1)
        {
            if (layer == -1)
            {
                return GetStateMachine().states.FirstOrDefault(s => s.state.name == name).state;
            }
            else
            {
                return asset.layers[layer].stateMachine.states.FirstOrDefault(s => s.state.name == name).state;
            }
        }
        
        /// <summary>
        /// 新たなレイヤーを作る
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public AnimatorController CreateLayer(string layerName = "")
        {
            if (!String.IsNullOrWhiteSpace(layerName))
            {
                asset.AddLayer(
                    new AnimatorControllerLayer()
                    {
                        avatarMask = null,
                        blendingMode = AnimatorLayerBlendingMode.Override,
                        defaultWeight = 1f,
                        iKPass =  false,
                        name = layerName,
                        stateMachine = new AnimatorStateMachine(),
                    });
                layer++;
            }
            return asset;
        }

        public AnimatorController Create()
        {
            foreach (var l in asset.layers)
            {
                AlignmentMachine(l.stateMachine);
            }
            return asset;
        }
        
        /// <summary>
        /// ディレクトリにアセットを作成する
        /// </summary>
        public AnimatorController CreateAsset(string path = null, bool subAsset = false)
        {
            Create();
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (subAsset)
                {
                    AssetDatabase.AddObjectToAsset(asset,path);
                }
                else
                {
                    if(!path.EndsWith(".controller")) path = Path.Combine(path, asset.name+".controller");
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(asset,path);
                }

                foreach (var l in asset.layers)
                {
                    SaveStateMachine(l.stateMachine, path);
                }
            }

            return asset;
        }

        void SaveStateMachine(AnimatorStateMachine machine,string path)
        {
            AssetDatabase.AddObjectToAsset(machine,path);
            foreach (var s in machine.states)
            {
                AssetDatabase.AddObjectToAsset(s.state,path);
                foreach (var t in s.state.transitions)
                {
                    AssetDatabase.AddObjectToAsset(t,path);
                }
            }

            foreach (var t in machine.entryTransitions)
            {
                AssetDatabase.AddObjectToAsset(t,path);
            }
            foreach (var t in machine.anyStateTransitions)
            {
                AssetDatabase.AddObjectToAsset(t,path);
            }
            foreach (var m in machine.stateMachines)
            {
                SaveStateMachine(m.stateMachine,path);
            }  
        }

        void AlignmentMachine(AnimatorStateMachine machine)
        {
            machine.exitPosition = new Vector3(0f,-100f,0f);
            
            Vector2 cursor = Vector3.zero;
            machine.entryPosition = cursor;
            cursor = AlignmentState(machine.defaultState, cursor, machine);

            machine.anyStatePosition = cursor;
            foreach (var transition in machine.anyStateTransitions)
            {
                cursor = AlignmentState(transition.destinationState, cursor, machine);
            }

            foreach (var state in machine.states)
            {
                cursor = AlignmentStateReversal(state.state,cursor,machine);
            }
        }

        Vector2 AlignmentState(AnimatorState state, Vector2 cursor, AnimatorStateMachine machine)
        {
            int index = Array.FindIndex(machine.states, s => s.state.name == state.name);
            var list = machine.states.ToList();
            if (machine.states[index].position.y < -999f)
            {
                list.Remove(machine.states[index]);
                cursor += new Vector2(300f, 0f);
                var childState = new ChildAnimatorState()
                {
                    state = state,
                    position = new Vector3(cursor.x,cursor.y,0f)
                };
                list.Add(childState);
                machine.states = list.ToArray();
                foreach (var transition in state.transitions)
                {
                    if (transition.destinationState != null)
                    {
                        cursor += new Vector2(0f, 100f);
                        cursor = AlignmentState(transition.destinationState, cursor, machine);
                    }

                    if (transition.isExit)
                    {
                        Vector2 newpos = cursor + new Vector2(300f, 0f);
                        newpos = new Vector2(Mathf.Max(newpos.x,machine.exitPosition.x),newpos.y);
                        machine.exitPosition = newpos;
                    }
                }
                cursor -= new Vector2(300f, 0f);
            }
            return cursor;
        }
        Vector2 AlignmentStateReversal(AnimatorState state, Vector2 cursor, AnimatorStateMachine machine)
        {
            int index = Array.FindIndex(machine.states, s => s.state.name == state.name);
            var list = machine.states.ToList();
            if (machine.states[index].position.y < -999f)
            {
                list.Remove(machine.states[index]);
                
                // 子State検知
                int c = -1;
                if (state.transitions != null)
                {
                    if (state.transitions.Length > 0)
                    {
                        if (state.transitions[0].destinationState != null)
                        {
                            c = Array.FindIndex(machine.states, s => s.state.name == state.transitions[0].name);
                        }
                    }
                }
                cursor += new Vector2(300f, 0f);
                var newPos = cursor;
                if (c > -1)
                {
                    newPos = new Vector2(machine.states[c].position.x-300f, machine.states[c].position.y+50f);
                }

                int p = Array.FindIndex(machine.states,
                    s => s.state.transitions.FirstOrDefault(
                        t => t.destinationState?.name == state.name) != null);
                
                // 重なり検知
                while (true)
                {
                    int o = Array.FindIndex(machine.states, s => Vector2.Distance(new Vector2(s.position.x, s.position.y), newPos)<10f);
                    if(o == -1) break;
                    newPos += new Vector2(-300f,0f);
                }

                // 孤立State
                if (p == -1 && state.transitions.Length == 0)
                {
                    newPos = new Vector2(Random.Range(-500f,500f),Random.Range(-900f,-200f));
                }
                
                var childState = new ChildAnimatorState()
                {
                    state = state,
                    position = new Vector3(newPos.x,newPos.y,0f)
                };
                list.Add(childState);
                machine.states = list.ToArray();
                foreach (var transition in state.transitions)
                {
                    if (transition.destinationState != null)
                    {
                        cursor += new Vector2(0f, 100f);
                        cursor = AlignmentState(transition.destinationState, cursor, machine);
                    }

                    if (transition.isExit)
                    {
                        newPos = cursor + new Vector2(300f, 0f);
                        newPos = new Vector2(Mathf.Max(newPos.x,machine.exitPosition.x),newPos.y);
                        machine.exitPosition = newPos;
                    }
                }
                cursor -= new Vector2(300f, 0f);
            }
            return cursor;
        }

        Vector2 RandomPosition()
        {
            var x = (float)Random.Range(-100, 100) *100f;
            var y = -1000f;
            var z = 0f;
            return new Vector3( x, y, z);
        }
    }
}
