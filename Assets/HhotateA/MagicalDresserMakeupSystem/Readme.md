# マジックドレッサーメイクアップ(MagicalDresserMakeupSystem)

VRChatのアバターで，メニューから服や髪色や，BlendShapeの上書きを設定できるツールです．

## 導入方法
1. あらかじめアバターアップロード用プロジェクトのバックアップを取っておく．
2. VRCSDK3-AVATARを最新版に更新する．
3. ItemPickupSetup.unitypackageをUnityProjectにインポートする．
4. Unityの上部メニュー，Window/HhotateA/アバターアイテムセットアップ(ItemPickupSetup)を開く．
5. "Avatar"の欄にシーン上のアバターオブジェクトをドラッグ&ドロップで参照する.
6. "Renderer"の欄にアバター内の手に持ちたいオブジェクトをドラッグ&ドロップで参照する.
7. ColorChangeの設定を行う．
    - None : 色改変を行わない
    - Texture : テクスチャを入れ替える色改変(安定)
    - RGB : グレースケールにRGBを乗算する色設定
    - HSV : HSVフィルターを用いた色設定
8. ShapeChangeの設定を行う
    - None : BlendShapeを設定しない．
    - Radial : 選択したBlendShapeを非段階的に設定する．
    - Toggle : 選択したBlendShapeそれぞれをトグルメニューで設定する
9. ”Setup”ボタンを押す．
10. 通常の手順でアバターをアップロードする．

## 使用方法
1. AvatarのExpressionMenuから"MagicalDresserMakeupSystem"メニューを開きアイテムを選択する．
2. 色，またはBlendShapeの値を設定できるฅ(＾・ω・＾ฅ)

## アンインストール手順
### v1.27以降
 1. 本ツールの"Modify Options"オプションから"Force Revert"ボタンを押す．
 2. 「Status : Complete Revert」というメッセージが出れば成功
### v1.26以前
1. Fx_Animatorから"MDMakeup_"から始まる名前のレイヤーを削除する．
2. VRCExpressionsMenuから"MDMakeup_"から始まる名前の項目を削除する．
3. VRCExpressionParameters"MDMakeup_"から始まる名前の項目を削除する．
4. HSV設定した場合，Rendererの子オブジェクトの"(clone)_Filter"という名前のオブジェクトを削除する

## Modify Options
- Override Write Default : WriteDefaultの値を上書きします．(VRChat非推奨項目)
- RenameParameters : パラメーター名に含まれる2バイト文字をハッシュ化して取り除きます．
- Auto Next Page : メニューの項目数が上限に達した場合，自動で次ページを作成します．

- Force Revert : このツールでセットアップされた設定を元に戻します．

## 注意事項
- アバターのfxAnimatorController,ExpressionMenu,ExpressionParametersに破壊的な変更を加えます．あらかじめ忘れずにバックアップを取ってください．
- ExpressionParameters,ExpressionMenuの項目が上限に達していた場合，正常に導入できない場合があります．その場合は一時的に項目を減らすなどの対処をお願い致します．
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
2021/08/13 v1.26
2021/08/27 v1.27
2021/09/03 v1.28