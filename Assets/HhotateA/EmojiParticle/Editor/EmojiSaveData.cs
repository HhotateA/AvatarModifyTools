using System.Collections.Generic;
using UnityEngine;

namespace HhotateA
{
    [CreateAssetMenu(menuName = "HhotateA/EmojiSaveData")]
    public class EmojiSaveData : ScriptableObject
    {
        public List<IconElement> Emojis = new List<IconElement>();
    }
    
    [System.Serializable]
    public class IconElement
    {
        public IconElement(string name, Texture emoji)
        {
            Name = name;
            Emoji = emoji;
        }

        [SerializeField] public string Name;
        [SerializeField] public Texture Emoji;

        public Texture2D ToTexture2D()
        {
            return Emoji as Texture2D;
        }
        
        public Texture ToTexture()
        {
            return Emoji as Texture;
        }
    }
}