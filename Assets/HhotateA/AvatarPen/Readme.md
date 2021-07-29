# AvatarPenSetUpTool

VRChatのアバターで使用できる，トレイルタイプの指ペンの簡易セットアップツールです．

## 導入方法
1. あらかじめアバターアップロード用プロジェクトのバックアップを取っておく．
2. VRCSDK3-AVATARを最新版に更新する．
3. AvatarPenSetupTool.unitypackageをUnityProjectにインポートする．
4. Unityの上部メニュー，Window/HhotateA/AvatarPenSetupを開く．
5. "Avatar"の欄にシーン上のアバターオブジェクトをドラッグ&ドロップで参照する
6. ”Setup”ボタンを押す．
7. 通常の手順でアバターをアップロードする．

## 使用方法
1. AvatarのExpressionMenuからPenを選択する．
2. 色を選択する．
3. 右手をFingerpointにしたときに，指先からトレイルが出ます．✒
4. PenMenuのEraseで消し消しฅ(＾・ω・＾ฅ)

## 注意事項
- アバターのfxAnimatorController,ExpressionMenu,ExpressionParametersに破壊的な変更を加えます．あらかじめ忘れずにバックアップを取ってください．
- ExpressionParameters,ExpressionMenuの項目が上限に達していた場合，正常に導入できない場合があります．その場合は一時的に項目を減らすなどの対処をお願い致します．

## 利用規約
- アバターへの同梱，改良，ツールの一部，まるごと含め，二次配布可とします．
- 二次配布する場合，連絡とクレジット表記があるとうれしいです．(必須ではありません)
- 本ツールを使用して発生した問題に対しては製作者は一切の責任を負いません.
- VRChatやUnity等の仕様変更により本ツールの機能が使えなくなった場合、製作者は責任を負いません。

## 制作者
@HhotateA_xR

## 更新履歴
2021/04/04 v0.9
2021/04/06 v1.1 EmojiParticleSetupToolに伴うAvatarModifyToolの破壊的アップデート
2021/07/08 v1.2 TextureModifyToolのリリースとAvatarModifityToolのアップデート