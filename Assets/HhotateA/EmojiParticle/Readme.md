# EmojiParticleSetupTool

VRChatのアバターで使用できる，絵文字パーティクルのセットアップツールです．
イラストのスタンプをVRChatの

## 導入方法
1. あらかじめアバターアップロード用プロジェクトのバックアップを取っておく．
2. VRCSDK3-AVATARを最新版に更新する．
3. EmojiParticleSetupTool.unitypackageをUnityProjectにインポートする．
4. Unityの上部メニュー，Window/HhotateA/EmojiParticleSetupを開く.
5. 出したい絵文字(スタンプイラスト)の画像とタイトルをすべて登録する．
6. "Avatar"の欄にシーン上のアバターオブジェクトをドラッグ&ドロップで参照する
7. パーティクルを出すところを(Head/LeftHand/RightHand)から選択する．
8. ”Setup”ボタンを押す．
9. 通常の手順でアバターをアップロードする．

## 使用方法
1. AvatarのExpressionMenuからEmojiParticleを選択する．
2. Emojiを選ぶとパーティクルを出せるよーฅ(＾・ω・＾ฅ)

## 注意事項
- アバターのfxAnimatorController,ExpressionMenu,ExpressionParametersに破壊的な変更を加えます．あらかじめ忘れずにバックアップを取ってください．
- ExpressionParameters,ExpressionMenuの項目が上限に達していた場合，正常に導入できない場合があります．その場合は一時的に項目を減らすなどの対処をお願い致します．
- VRChatのミラーにパーティクルが映らないことがあります．目視またはカメラで確認してください．
- AvatarPenSetupと競合してエラーが出る場合はFullPackageを試してください．

## 利用規約
- アバターへの同梱，改良，ツールの一部，まるごと含め，二次配布可とします．
- 二次配布する場合，連絡とクレジット表記があるとうれしいです．(必須ではありません)
- 本ツールを使用して発生した問題に対しては製作者は一切の責任を負いません.
- VRChatやUnity等の仕様変更により本ツールの機能が使えなくなった場合、製作者は責任を負いません。

## 制作者
@HhotateA_xR

## 更新履歴
2021/04/05 v1.0
2021/07/08 v1.2 TextureModifyToolのリリースとAvatarModifityToolのアップデート
2021/07/31 v1.25
2021/08/13 v1.26