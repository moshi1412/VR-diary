# VR DIARY PROJECT FOR GROUP WORK

## 环境配置

1. 在untiy官网下载hub  在hub里下载editor 下载时选择 Android平台的包

> 项目开始的步骤参考视频： （用github实现团队协作）
> 
> https://www.bilibili.com/video/BV1xEUXYHE8K/?spm_id_from=333.1391.0.0

我不太会pull和push，所以最好下载一个github desktop 直观一点

具体的文件传输什么的 ，如果文件大的话，可能还是只能用微信传输

2. unity内部包的环境配置 包括openxr的设置 和 meta sdk all-in-one（老师课上讲的）的包（这个要上asset store找一下） 

> 视频链接（亲测有用）
>
> https://www.bilibili.com/video/BV1QdVYzcEk9/?spm_id_from=333.1391.0.0&vd_source=de85285ffc34d365d7e48317702d6f30
>
视频中提到的直接用头显进行串流调试 下载 meta quest link 软件就可以（亲测成功）

 但是如果不想用头显，也可以直接使用all-in-one里面的meta simulator的组件，那个我还不太会用
 
> **meta quest link 登陆时会出现网络的问题 这是正常的**
>
> 解决方法：（**下面一些评论也挺有用**）
>
> https://www.bilibili.com/opus/555041737514441795

> 软件里面设置：（视频的 00：40-1：22）
>
> https://www.bilibili.com/video/BV1Ch411M7EZ/?spm_id_from=333.1391.0.0&vd_source=de85285ffc34d365d7e48317702d6f30

**软件里面要打开设置-beta测试版-公测渠道，**

我的开完就直接更新了，然后选项就多了
 
好像还要注册meta的开发者账号（没试过不注册会怎么样）
> 开启双重验证（也可以找一下网上的视频，开启账号开发者模式的）：
> 
> https://accountscenter.meta.com/password_and_security
>
>注册开发者
>
> https://developers.meta.com/horizon/sign-up/


## 项目进度

10.23 实现串流调试 建立仓库

10.24 提出初步构想
> 全景场景搭建
>
> https://www.bilibili.com/video/BV1F4hzemEM5?spm_id_from=333.788.player.player_end_recommend_autoplay&vd_source=de85285ffc34d365d7e48317702d6f30&trackid=web_related_0.router-related-2206419-xc9bj.1761318290883.815
>
> 只要找到合适的VR全景图片就行

10.25 使用星空的全景图，直接把材质用在场景设置里的skybox上就行了

10.27 查找免费模型的书架，放在场景中，但是现在虽然加了刚体组件，不会因为重力掉落，很奇怪的bug，但是OVRCameraRig也是这样，后面再修。

11.1 实现控制器进行按钮的点击，实现录音功能，保存在电脑本地，具体路径需要查看unity console 里面的输出。

上传了第一个版本，之后开一个branch做后面的版本
<<<<<<< Updated upstream
=======

11.5

1. 记忆球放在架子上，随机有一些发光
2. 靠近拾取记忆球，视频在背景上显示（？）
3. 拿在手上时会出现按钮   功能：添加录音 添加照片  添加tag  
4. 拿在手上时会出现标签（文本框）
5. 新建记忆：球从导轨中滚出

11.8 

1. 导轨已建好，动力模拟实现
2. 决定使用控制台，控制台中间凹槽放置记忆球
3. 控制台实体按钮：由两个圆柱组成，在手压到按钮上时，按钮会下降，手指松开时按钮回弹（使用SpringJoint连接两个圆柱）
4. 虚拟按钮按钮相比屏幕，我更像做一个UI一样的内容，所以现在是悬浮屏幕
5. 球放在控制台中间时是悬浮的（未实现）

> **重要：**
>
> 1. 记忆球我做成了预制体保存在了prefab文件夹下，生成球的思路是 手指（需要用hand tracking功能包，我应该已经加到OVRCamera下面了）碰到按钮的碰撞体时会触发脚本，生成ball
>
> 2. 现在datamanager脚本的逻辑是把记忆的数据存到本地（json）（使用内部JsonUtility库）

<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
