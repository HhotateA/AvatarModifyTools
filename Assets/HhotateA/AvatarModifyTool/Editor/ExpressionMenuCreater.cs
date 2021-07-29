using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    public class ExpressionMenuCreater : MonoBehaviour
    {
#if VRC_SDK_VRCSDK3
        /// <summary>
        /// ExpressionsMenuの安全な結合
        /// </summary>
        /// <param name="menus"></param>
        /// <param name="origin"></param>
        VRCExpressionsMenu AddyExpressionMenu(VRCExpressionsMenu menus, string name,Texture2D icon,float value)
        {
            menus.controls.Add(new VRCExpressionsMenu.Control()
            {
                name = name,
                icon = icon,
                parameter = new VRCExpressionsMenu.Control.Parameter()
                {
                    name = "",
                },
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                value = value,
                style = VRCExpressionsMenu.Control.Style.Style1,
                subMenu = null,
                subParameters = null,
                labels = null,
            });
            return menus;
        }
#endif
    }
}