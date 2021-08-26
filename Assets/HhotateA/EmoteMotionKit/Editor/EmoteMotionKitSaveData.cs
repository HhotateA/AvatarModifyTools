/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HhotateA.AvatarModifyTools.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HhotateA.AvatarModifyTools.EmoteMotionKit
{
    [CreateAssetMenu(menuName = "HhotateA/EmoteMotionKitSaveData")]
    public class EmoteMotionKitSaveData : ScriptableObject
    {
        [FormerlySerializedAs("name")] public string saveName = "EmoteMotionSaveData";
        public Texture2D icon;
        public List<EmoteElement> emotes = new List<EmoteElement>();
    }
    
    [System.Serializable]
    public class EmoteElement
    {
        public string name;
        public Texture2D icon;
        public AnimationClip anim;
        public bool isEmote;
        public bool locomotionStop;
        public bool poseControll;
        public TrackingType tracking;
    }

    public enum TrackingType
    {
        HeadTracking,
        HandTracking,
        FullTracking,
        Animation,
    }
}