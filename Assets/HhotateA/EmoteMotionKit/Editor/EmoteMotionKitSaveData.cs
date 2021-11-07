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
        public bool isSaved = false;
        public EmoteLayer emoteLayer = EmoteLayer.Base;
        public bool copyToFXLayer = false;
    }
    
    [System.Serializable]
    public class EmoteElement
    {
        public string name = "Emote";
        public Texture2D icon;
        public AnimationClip anim;
        public bool isEmote = false;
        public bool locomotionStop = false;
        public bool poseControll = false;
        public TrackingSpace tracking = TrackingSpace.Emote;
        public AnimationSettings animationSettings;

        public EmoteElement(Texture2D tex)
        {
            icon = tex;
        }
    }

    public enum TrackingSpace
    {
        TrackingBase,
        FootAnimation,
        BodyAnimation,
        AnimationBase,
        Emote
    }

    public enum EmoteLayer
    {
        Base,
        Action,
        Additive,
    }

    public enum AnimationSettings
    {
        Emote,
        Animation,
        Pose,
        Locomotion,
        Custom
    }
}