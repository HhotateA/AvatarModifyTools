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

namespace HhotateA.AvatarModifyTools.EmojiParticle
{
    [CreateAssetMenu(menuName = "HhotateA/EmojiSaveData")]
    public class EmojiSaveData : ScriptableObject
    {
        [FormerlySerializedAs("name")] public string saveName = "";
        public List<IconElement> emojis = new List<IconElement>();
        public AvatarModifyData assets;
    }
    
    [System.Serializable]
    public class IconElement
    {
        public IconElement(string name, Texture emoji)
        {
            this.name = name;
            this.emoji = emoji;
        }

        public string name;
        public Texture emoji;
        public int count;
        public float lifetime = 2f;
        public float scale = 0.4f;
        public float speed = 0f;
        public GameObject prefab;
        public AudioClip audio;


        public Texture2D ToTexture2D()
        {
            return emoji as Texture2D;
        }
        
        public Texture ToTexture()
        {
            return emoji as Texture;
        }
    }
}