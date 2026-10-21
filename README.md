# TextbookSpider

国家中小学智慧教育平台教材 PDF 下载工具，包含命令行和 WPF Windows 界面，共用 `TextbookSpiderBase` 的目录解析和下载逻辑。

## 运行环境

- Windows 和 .NET 10 SDK（构建）；运行 WPF 需要 .NET 10 Windows Desktop Runtime。
- 有效的 `x-nd-auth` 令牌（下载教材时使用）。

```powershell
dotnet build TextbookSpider.sln
dotnet run --project TextbookSpiderWpf
# 命令行版本
dotnet run --project TextbookSpiderCli
```

## WPF 使用

1. 启动后自动加载目录；加载失败时查看日志并点击“刷新”。目录中的三个数据分片全部加载成功后才会更新界面。
2. 在左侧展开并选择分类，右侧预览该分类及下级目录中的教材。
3. 选择保存位置并填写自己的 `x-nd-auth` 令牌。令牌输入会遮蔽，应用不将令牌保存到磁盘。
4. 点击“开始下载”，PDF 按所选分类的目录结构保存。界面显示已处理数量、成功和失败数量，详细错误见运行日志。
5. 可随时取消。已完成的 PDF 保留，未完成的临时文件会清理。关闭窗口时先取消正在执行的任务，再退出。

WPF 默认保存到程序目录下的 `Textbooks`，运行日志写入程序目录中的 `log.txt`；CLI 仍使用当前工作目录。再次下载会替换同路径的文件，但只有新文件完整写入后才替换旧文件。无法用于 Windows 文件名的字符会被替换为下划线。

目录服务目前使用 `part_101.json` 至 `part_103.json`，与原命令行项目一致。真实下载需要网络和有效令牌。
