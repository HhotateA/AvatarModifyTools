# アバターアイテムセットアップ(GrabableItemSetup)

VRChatのアバターで，アイテムを手に持ったり，ワールドに置いたりすることのできるギミックのセットアップツールです．

## 導入方法
1. あらかじめアバターアップロード用プロジェクトのバックアップを取っておく．
2. VRCSDK3-AVATARを最新版に更新する．
3. ItemPickupSetup.unitypackageをUnityProjectにインポートする．
4. Unityの上部メニュー，Window/HhotateA/アバターアイテムセットアップ(ItemPickupSetup)を開く．
5. "Avatar"の欄にシーン上のアバターオブジェクトをドラッグ&ドロップで参照する.
6. "Object"の欄にアバター内の手に持ちたいオブジェクトをドラッグ&ドロップで参照する.
7. "HandBone"に手のオブジェクトをドラッグ&ドロップで参照，右側の欄で手に持つトリガーとなるハンドサインを設定する．
8. "WorldBone"の右側の欄で手に持つトリガーとなるハンドサインを設定する．
    - Questで使用する場合はUseConstraintをオフにする．
    - ボーンを対象とする場合はSafeOriginalItemをオフにする．
9. ”Setup”ボタンを押す．
10. 通常の手順でアバターをアップロードする．

## 使用方法
1. AvatarのExpressionMenuから"GrabableItem"メニューを開き，アイテムを選択する．
2. Grabを選択（＋設定したハンドアクションをする）ことでアイテムを手に持てます．
3. Dropを選択（＋設定したハンドアクションをする）ことでアイテムをワールド固定できます．

## アンインストール手順
### v1.27以降
 1. 本ツールのVRChatNotRecommendedオプションから"Force Revert"ボタンを押す．
 2. 「Status : Complete Revert」というメッセージが出れば成功
### v1.26以前
 1. Fx_Animatorから"GrabableItem_"から始まる名前のレイヤーを削除する．
 2. VRCExpressionsMenuから"GrabableItem_"から始まる名前の項目を削除する．
 3. VRCExpressionParameters"GrabableItem_"から始まる名前の項目を削除する．
 4. アバター直下の"WorldPoint"オブジェクトを削除する．
 5. アバターの手ボーンの"HandAnchor_"オブジェクトを削除する．
 5. アバター下アイテムと同じ階層にあるの"RootAnchor_"オブジェクトを削除する．

## 注意事項
- アバターのfxAnimatorController,ExpressionMenu,ExpressionParametersに破壊的な変更を加えます．あらかじめ忘れずにバックアップを取ってください．
- ExpressionParameters,ExpressionMenuの項目が上限に達していた場合，正常に導入できない場合があります．その場合は一時的に項目を減らすなどの対処をお願い致します．
- 過去バージョンと競合してエラーが出る場合はFullPackageを試してください．

## 利用規約
- アバターへの同梱，改良，ツールの一部，まるごと含め，二次配布可とします．
- 二次配布する場合，連絡とクレジット表記があるとうれしいです．(必須ではありません)
- 本ツールを使用して発生した問題に対しては製作者は一切の責任を負いません.
- VRChatやUnity等の仕様変更により本ツールの機能が使えなくなった場合、製作者は責任を負いません。

## 制作者
@HhotateA_xR
問題報告は https://github.com/HhotateA/AvatarModifyTools へ

## 更新履歴
2021/08/13 v1.26
2021/08/27 v1.27