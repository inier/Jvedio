# JvedioUpdate

Jvedio 的更新程序

版本：1.0.0.0

主要功能：

---

1. 读取 http://hitchao.gitee.io/jvedioupdate/Version 文件中的内容，判断是否有更新版本
2. 读取 http://hitchao.gitee.io/jvedioupdate/list 列表，与本地文件进行 md5 校验
3. 校验不一致的或者不存在的文件，下载并覆盖，执行更新
4. 更新完成后重启 Jvedio