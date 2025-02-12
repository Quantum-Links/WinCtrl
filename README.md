# WinCtrl
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/dotnet/winforms/blob/main/LICENSE.TXT)

udp端口默认为10001

发送任意命令都会回复success

软件默认会开机自启，自启方式为添加注册表启动项

控制命令

>shutdown    -关闭电脑
>
>restart     -重启电脑
>
>volume0     -音量调整至0
>
>volume100   -音量调整至100 
>
>volumeup   -音量增加5
>
>volumedown   -音量减少5
>
>playvideo xxx  -播放本机xxx路径视频
> 
> pause -暂停视频
> 
> play -继续播放
> 
> hidevideo -隐藏视频播放
> 
> setvideo 0.1 -设置视频播放进度百分比 0.1就是10%