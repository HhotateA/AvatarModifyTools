# アバターペンセットアップ(AvatarPenSetup)

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
3. 右手をFingerpointにしたときに，指先からトレイルが出ます．
4. PenMenuのEraseで消し消しฅ(＾・ω・＾ฅ)

## アンインストール手順
1. Fx_Animatorから"AvatarPen"から始まる名前のレイヤーを削除する．
2. VRCExpressionsMenuから"AvatarPen"から始まる名前の項目を削除する．
3. VRCExpressionParameters"AvatarPen"から始まる名前の項目を削除する．
4. アバター指先の"Avatar_Pen"オブジェクトを削除する．

## Modify Options
- Override Write Default : WriteDefaultの値を上書きします．(VRChat非推奨項目)
- RenameParameters : パラメーター名に含まれる2バイト文字をハッシュ化して取り除きます．
- Auto Next Page : メニューの項目数が上限に達した場合，自動で次ページを作成します．

- Force Revert : このツールでセットアップされた設定を元に戻します．

## 注意事項
- アバターのfxAnimatorController,ExpressionMenu,ExpressionParametersに破壊的な変更を加えます．あらかじめ忘れずにバックアップを取ってください．
- 過去バージョンと競合してエラーが出る場合はFullPackageを試してください．

## 利用規約
- アバターへの同梱，改良，ツールの一部，まるごと含め，二次配布可とします．
- 二次配布する場合，連絡とクレジット表記があるとうれしいです．(必須ではありません)
- 本ツールを使用して発生した問題に対しては製作者は一切の責任を負いません.
- VRChatやUnity等の仕様変更により本ツールの機能が使えなくなった場合、製作者は責任を負いません。

## 動作確認環境
- Unity2019.4.24f1
- VRCSDK3-AVATAR-2021.08.11.15.16_Public

## 制作者
@HhotateA_xR
問題報告は https://github.com/HhotateA/AvatarModifyTools へ

## 更新履歴
2021/04/04 v0.9
2021/04/06 v1.1 EmojiParticleSetupToolに伴うAvatarModifyToolの破壊的アップデート
2021/07/08 v1.2 TextureModifyToolのリリースとAvatarModifityToolのアップデート
2021/07/31 v1.25
2021/08/13 v1.26
2021/08/27 v1.27
2021/09/03 v1.29