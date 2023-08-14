﻿# 接口指标采集
通过curl、ping、telnet、cmd等多种方式采集目标数据，用于健康度检测

## 编译
> 因为需要将Plugins的项目输出复制到主应用中，请编译整个解决方案

## 部署

直接执行：`dotnet MCollector.dll` 即可，或以独立模式打包为可执行程序

### 容器方式
`docker build .`

### windows服务方式
使用 `install.bat` 或 `uninstall.bat`脚本来安装或卸载windows服务

## 运营接口
> 需在配置文件`api`中启用


1. `http://[ip:port]/status` 查看所有采集结果

2. `http://[ip:port]/refresh` 触发重新采集

## 配置
配置文件为`collector.yml`，示例如下：

```yaml
port: 18086 # 应用提供服务的端口
api: 
  status: true # 是否启用status接口
  refresh: true # 是否启用刷新接口，若启用，则可以通用GET /refresh接口，立即重新检测所有目标
exporter: # 检测结果导出，，如：prometheus、邮件通知等等
  prometheus: # prometheus的自定义配置
    enable: true
    port: 1234
  email:
    host: 0:21
targets: # 检测目标集合（target）
  - name: curl local # 名称
    target: "http://127.0.0.1" # 目标
    type: url # 采集方式
    interval: 3 # 检测间隔时间，单位：s，默认3s，也可使用ms,m,h,rand(10s,20s),rand(10s)等单位
    headers: # 【可选】头信息，键值对
      Host: xxx.com
      Content-Type: application/json
      [...]
    contents: # 【可选】提交的内容，数组，如req body或commands
      - "{test:1}"
      - "{test:2}"
    transform: # 【可选】使用多个转换器对采集数据转换，如对返回的内容做文本查找等
      search: # 转换器名称，可自定义，需实现ITransformer接口
        text: "success" # 转换器自定义配置
      other: # 其它转换器串联
        params: xxx
      [...]
```

### target配置说明
| 字段 | 类型 | 说明 |
|-|-|-|
| name | string | 名称 |
| target | string | 目标地址 |
| type | string | 使用的Collector名称 |
| internval | string | 间隔时间，默认秒，可使用字母单位，如：ms(毫秒)、s(秒)、m(分钟)、h(小时) 、rand（随机数），如：rand(10s，20s)表示大于等于10秒小于20s的间隔时间、rand(20s)表示在20s上下10%内浮动 |
| headers | dictionary | 请求头 |
| contents | string[] | 请求消息体 |
| transform | dictionary | 结果转换 |

## 采集方式
### `type: url`
使用http GET的方式请求目标
1. 如配置了contents，将改用POST，并将Contents数据内容按字符拼接后放入request body
1. 会自动跟踪302跳转
1. 若返回http code大于400，则判断为失败

``` yaml
  - name: name # 名称
    target: "http://..." # url地址
    type: url
    interval: 3
    headers: # 【可选】自定义http头
      Host: www.baidu.com
      Content-Type: application/json
    contents: # 【可选】提交的内容，若有值，会使用post方式，并将所有contents放入body中
      - "{test:1}"
```

### `type: ping`
``` yaml
  - name: name # 名称
    target: "ip" # ip
    type: ping
    interval: 3
```

### `type: telnet`
``` yaml
  - name: name # 名称
    target: "ip:port" # ip和port
    type: telnet
    interval: 3
```
> 暂未实现将Contents内容发送到服务端

### `type: cmd`
在一个会话中逐条执行Contents中的命令行，~~任一语句执行失败则中止~~
``` yaml
  - name: name # 名称
    target: "" # 【可选】 win默认`cmd` linux默认`bin/bash`
    type: cmd
    interval: 3
    contents:
      - openssl s_client --connect www.baidu.com
      - echo ok
```
> 一般会配合transformer使用，如检测web服务是否支持tls1.2

### 自定义采集方式
需实现ICollector接口，如实现一个腾讯云的信息采集：
``` csharp
    public class MainCollector : ICollector
    {
        public string Type => "tc";

        public Task<CollectedData> Collect(CollectTarget target)
        {
            throw new NotImplementedException();
        }
    }
```
将上面的编译dll放入Plugins目录中，再在collector.yml中添加如下即可（会自动将yml注入到参数中）：

``` yaml
  - name: tencent cloud demo
    target: "http://xxx/tke/api"
    type: tc
    interval: 30
```

## 预处理(prepare)
### OAuth
> todo

## 转换器(transformer)
可以使用多个tranformer串联执行，当CollectedData的IsSuccess为false或标记为Final时，中止执行后续transformer
### `json`
将采集的内容以json格式解析，如果返回内容是数组类型，则会将转换应用到数组中的每个对象上，这种情况下会生成多条采集结果
```yaml
    transform:
      json: 
        rootPath: results # 【可选】 改为解析的根，如以data.items[0]的值为根节点输入到后续extract
        extractNameFrom: name # 将json对象中哪个属性映射name，默认从name提取
        extractNameFromProperty: false # 如果为true, 则将json对象所有属性提取为kv，忽略extractNameFrom配置。默认false
        extractContentFrom: msg # 将json对象中哪个属性映射content，默认从content提取
        contentMapper: # 对获取的值替换，如将Healthy替换为0
          Healthy: 1
          Unhealthy: 0
```


### `search`
搜索返回内容中是否包含指定字符串，
> 这个transform会将结果标记为中止，不再继续执行后续transform
```yaml
    transform: 
     search:
       text: xxxx # 需要搜索的字符串内容
```

### `targets`
将收集到的信息转换为target并合并到本地配置中，主要用于动态加载targets配置
1. 如果返回是内容以`{`或`[`字符开头，会以json反序列化，否则使用yml反序列化
1. 默认忽略返回的cmd类型收集器
1. 该转换会覆盖本地的target配置

``` ymal
  - name: merge config
    target: http://localhost/config_mc
    type: url
    interval: 5000ms
    transform:
      targets:
        rootPath: data # 仅json格式时有效，yml默认targets
      targets: null
```

### 自定义转换
实现`ITransformer`接口即要，提供了`TransformerBase`和`TransformerBase<T>`两个基类简化实现，后续可使用强类型的参数，前者默认使用`Dictionary<string,object>`的类型参数
``` csharp
internal class CustomTransformer : TransformerBase<CustomTransformerArgs>
    {
        public override string Name => "custom";

        public override bool Transform(CollectedData rawData, CustomTransformerArgs args, out IEnumerable<CollectedData> results)
        {
            //换收集到的Data转换为一个或多个结果后，放入results变量

            return true;//是否转换成功
        }
    }

    internal class CustomTransformerArgs
    {
        public string Other1 { get; set; }
        public string Other2 { get; set; }
    }
```
将以上放和Plugin目录后，即可使用以下配置启用访转换器
```yaml
    transform: 
     custom:
       other1: xxxx
       other2: xxxx
```

## 导出(exporter)
所有导出方式是在ICollector和ITransformer执行完成后才触发
### prometheus
将采集到的数据上报到prometheus，上报的key为target的name，value的处理方式如下：
1. 如采集到的Content是数值，则转为double后上报
1. 如采集到的Content是true/false，则转为1/0上报
1. 其它情况以是否采集成为IsSuccess，转为1/0上报

配置说明如下：
``` yaml
  prometheus:
    enable: true # 是否启用
    port: 1234 # 供prometheus拉通数据的本地接口
```

### 自定义导出
实现`IExporter`接口，放入Plugin目录即可自动加载到运行时，如：
``` csharp
internal class CustomExporter : IExporter, IDisposable, IAsSingleton, IObserver<CollectedData>
{
    //使用ICollectedDataPool订阅结果
    public string Name => "abc";//对应到epxorts配置节中，名称是abc的下级配置类内，会以Dictionary传入Start方法

    public Task Start(Dictionary<string, object> args)
    {
        //使用SerializerHelper.Deserialize或CollectorConfig.Exports.GetConfig<T>将args转为强类型
    }

}
```


## 插件
MetricsCollector会自动加载Plugins目录下的所有dll，如可实现`ICollector`、`IExporter`、`ITransformer`等接口来自定义检测方式、数据转换等

## Dev说明

### `CollectedData`

| 属性名 | 类型 | 说明 |
|-|-|
| Name | String | Target的Name，若使用了Transformer，可能是Name+子元素Name |
| Target | String | 原始目标 |
| IsSuccess | bool | 采集是否成功 |
| Headers | string[] | 采集到的头信息 |
| Content | string | 采集到的内容 |
| Duration | long | 采集耗时，ms |
| LastCollectTime | DateTime | 最后执行时间 |

### `ICollectedDataPool`
获取所有已采集到的数据，实现了IObservable，也可使用订阅模式

##示例