# Yuduan.Redis
a library extends [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis). 基于StackExchange.Redis的封装库

Supports

 * .Net Standard 2.0 or above
 * .Net Framework 4.6.1 or above

[![NuGet version (Yuduan.Redis)](https://img.shields.io/nuget/v/Yuduan.Redis.svg?style=flat-square)](https://www.nuget.org/packages/Yuduan.Redis/)

Install by [nuget](https://www.nuget.org/packages/Yuduan.Redis)

    Install-Package Yuduan.Redis

### Simple Usage
```csharp
    RedisContext redis = new RedisContext("127.0.0.1:6379,allowAdmin=true,ssl=false");
    // Default is use database 0.
    var keyVal =  await redis.Default.StringGetAsync("key");
    // change database to 1 and get cache 
    redis.GetDatabase(1).GetAsync("key.1", () =>
    {
        //if not cached, it will get data from here
        return string.Empty;
    },TimeSpan.FromHours(1));     
```
### Reference
[Configuration Options](https://stackexchange.github.io/StackExchange.Redis/Configuration#configuration-options)

[MORE](https://stackexchange.github.io/StackExchange.Redis/#documentation)
