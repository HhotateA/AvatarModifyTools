using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace MyNamespace
{
    public class AnimatorControllerGenerator
    {
        AnimatorStateMachine CreateAnimatorController(AnimatorStateMachine stateMachine,
            AnimationState fromState,AnimationState toState)
        {
            var from = stateMachine.states.Select(s => s.state.name == fromState.name);
            var to = stateMachine.states.Select(s => s.state.name == fromState.name);
            var transition = new AnimatorStateTransition()
            {
                exitTime = 0,
            };
            return null;
        }
    }
}
