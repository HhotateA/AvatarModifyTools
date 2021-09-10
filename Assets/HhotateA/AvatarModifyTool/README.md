# AvatarModifyTools

VRChatのアバター改変を支援するためのいくつかのエディタ拡張です．

Here are some tools to help you modify your VRChat avatar.

### AvatarModifyTool
 アバター改変の手順を自動化し，ワンクリックでアバター改変を適応するツールです．
 Unity拡張を1から作らなくても，アクセサリーやアバターギミックのワンクリックセットアップツールをつくれるライブラリとしての使用を想定しています．
 
### AvatarModifyData
  アバターの改変内容を保存するためのアセットファイルです．
 
 - Locomotion_controller : Avatar3.0のBaseレイヤーに追加したいレイヤーを含むアニメーターコントローラーです．
 - idle_controller : Avatar3.0のAdditiveレイヤーに追加したいレイヤーを含むアニメーターコントローラーです．
 - gesture_controller : Avatar3.0のGestureレイヤーに追加したいレイヤーを含むアニメーターコントローラーです．
 - action_controller : Avatar3.0のActionレイヤーに追加したいレイヤーを含むアニメーターコントローラーです．
 - fx_controller : Avatar3.0のFxレイヤーに追加したいレイヤーを含むアニメーターコントローラーです．
 
 - parameter : Avatar3.0のパラメータードライバーに追加したい項目です．
 - menu : Avatar3.0のメニューに追加したい項目です．サブメニューに派生させることができます．
 
 - Item : アバターTransform内に追加したいGameobjectです．
    - prefab : 生成するするPrefabです．prefabのrootにParentConstraintがついている場合Constraintによる接続に切り替わり，prefabはAvatar直下に生成されます．
    - target : 対応するアバター側のボーンです．
  
    この機能を使いプレハブをアバターのボーンに差し込んだ場合，上記AnimatorControllerで参照しているすべてのアニメーションのパスが正しく書き換わります．<br>
  例えば，AvatarRoot直下の"AvatarPen"オブジェクトのオンオフをするアニメーションがあり，"AvatarPen"をこの機能で指先に移動させた場合，Animationのパスが自動で書き換わり，正常に動作し続けます．
 
 ### サンプルコード
 
 ```c#
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

public class TestSetupTool : EditorWindow
{
    [MenuItem("Window/TestSetupTool")]

    public static void ShowWindow()
    {
        var wnd = GetWindow<TestSetupTool>();
        wnd.titleContent = new GUIContent("TestSetupTool");
    }

    private VRCAvatarDescriptor avatar;
    
    // AvatarModifyDataのGUIDをここに代入
    const string assetGUID = "e5cd1ff11d13fed44bd5d0b8b4a2be8c";

    private void OnGUI()
    {
        avatar = (VRCAvatarDescriptor) EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);
        
        if (GUILayout.Button("Setup"))
        {
            var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(assetGUID);
            var mod = new AvatarModifyTool(avatar);
            mod.ModifyAvatar(asset);
        }
        
        EditorGUILayout.LabelField("powered by AvatarModifyTool @HhotateA_xR");
    }
}
```

## Lisence 
本ツールのすべてのソースコードはフリーライセンスとして公開されています．
- 再配布：可能
- 改変：可能
- 商用利用：可能
- 作者表記：不要
- ライセンスの継承：不要
- 販売アバターへの同梱，派生ツール，ツールの一部，まるごと含め，二次配布可とします．
- 二次配布する場合，連絡とクレジット表記があるとうれしいです．(必須ではありません)
- 本ツールを使用して発生した問題に対しては製作者は一切の責任を負いません.
- VRChatやUnity等の仕様変更により本ツールの機能が使えなくなった場合、製作者は責任を負いません。

## 動作確認環境
- Unity2019.4.24f1
- VRCSDK3-AVATAR-2021.08.11.15.16_Public

## 更新履歴

2021/04/04 v0.9<br>
2021/04/06 v1.1<br>
2021/07/08 v1.2<br>
2021/07/31 v1.25<br>
2021/08/13 v1.26<br>
2021/08/27 v1.27<br>
2021/09/03 v1.29<br>
2021/09/10 v1.30<br>