using UnityEditor.Animations;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace HhotateA
{
    [CreateAssetMenu(menuName = "HhotateA/AvatarModifyData")]
    public class AvatarModifyData : ScriptableObject
    {
        public AnimatorController locomotion_controller;
        public AnimatorController idle_controller;
        public AnimatorController gesture_controller;
        public AnimatorController action_controller;
        public AnimatorController fx_controller;
        public GameObject prefab;
        public HumanBodyBones target;
#if VRC_SDK_VRCSDK3
        public VRCExpressionParameters parameter;
        public VRCExpressionsMenu menu;
#endif
        
    }
}