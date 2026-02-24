using System;
using System.Collections.Generic;
using System.Text;

namespace FreeRedis.AliyunTair
{
    /// <summary>
    /// 扩展阿里云Tair Redis 数据类型接口
    /// </summary>
    public interface ITairRedisClient : IRedisClient
    {

        #region Hash类型

        /// <summary>
        /// 向Key指定的TairHash中插入一个field。如果TairHash不存在则自动创建一个，如果field已经存在则覆盖其值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field。</param>
        /// <param name="value">field对应的值，一个field只能有一个value。</param>
        /// <param name="timeout">相对过期过期时间,单位为秒,不传此参数表示不过期</param>
        /// <returns></returns>
        long ExHSet<T>(string key, string field, T value, TimeSpan? timeout = null);
        /// <summary>
        /// 同时向key指定的TairHash中插入多个field，如果TairHash不存在则自动创建一个，如果field已经存在则覆盖其值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field。</param>
        /// <param name="value">field对应的值，一个field只能有一个value。</param>
        /// <param name="fieldValues">field,valude数组 例如： field1,value1,field2,value2......</param>
        bool ExHMSet<T>(string key, string field, T value, params object[] fieldValues);
        /// <summary>
        /// 同时向key指定的TairHash中插入多个field，如果TairHash不存在则自动创建一个，如果field已经存在则覆盖其值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="keyValues">field,valude字典</param>
        bool ExHMSet<T>(string key, Dictionary<string, T> keyValues);
        /// <summary>
        /// 获取key指定的TairHash中一个field的值，如果TairHash不存在或者field不存在，则返回nil
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field。</param>
        /// <returns></returns>
        string ExHGet(string key, string field);
        /// <summary>
        /// 获取key指定的TairHash中一个field的值，如果TairHash不存在或者field不存在，则返回nil。 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        T ExHGet<T>(string key, string field);
        /// <summary>
        /// 同时获取key指定的TairHash多个field的值，如果TairHash不存在或者field不存在，则返回nil。
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="fields">多个field数组</param>
        /// <returns></returns>
        string[] ExHMGet(string key, params string[] fields);
        /// <summary>
        /// 同时获取key指定的TairHash多个field的值，如果TairHash不存在或者field不存在，则返回nil。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="fields">多个field数组</param>
        /// <returns></returns>
        T[] ExHMGet<T>(string key, params string[] fields);
        /// <summary>
        /// 在指定的key的TairHash中为一个field设置相对过期时间 
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field</param>
        /// <param name="expireTimeSpan">过期时间</param> 
        /// <param name="isMillisecond">是否精确到毫秒  默认为false:精确到秒  true：精确到毫秒</param>
        /// <returns></returns>
        bool ExHExpireTime(string key, string field, TimeSpan expireTimeSpan, bool isMillisecond = false);
        /// <summary>
        /// 在指定的key的TairHash中为一个field设置绝对时间 
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field</param>
        /// <param name="expireTime">绝对时间</param> 
        /// <param name="isMillisecond">是否精确到毫秒  默认为false:精确到秒  true：精确到毫秒</param>
        /// <returns></returns>
        bool ExHExpireTime(string key, string field, DateTime expireTime, bool isMillisecond = false);
        /// <summary>
        /// 查看key指定的TairHash中一个field的剩余过期时间，结果精确到毫秒。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns>
        /// <para>field存在但是没有设置过期时间：-1。</para>
        /// <para>key不存在：-2。</para>
        /// <para>field不存在：-3。</para> 
        /// <para>field存在且设置了过期时间：过期时间，单位为秒。</para>
        /// </returns>
        long ExHPTtl(string key, string field);
        /// <summary>
        /// 查看key指定的TairHash中一个field的过期时间，结果精确到秒。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns>
        /// <para>field存在但是没有设置过期时间：-1。</para>
        /// <para>key不存在：-2。</para>
        /// <para>field不存在：-3。</para>
        /// <para>field存在且设置了过期时间：过期时间，单位为秒。</para>
        /// </returns>
        long ExHTtl(string key, string field);
        /// <summary>
        /// 将key指定的TairHash中一个field的value增加num，num为一个整数。如果TairHash不存在则自动新创建一个，如果指定的field不存在，则在加之前插入该field并将其值设置为0
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field</param>
        /// <param name="num">需要为field的value增加的整数值</param>
        /// <param name="timeout">相对过期过期时间,单位为秒,不传此参数表示不过期</param>
        /// <returns>成功会返回：与num相加后value的值</returns>
        long ExHIncrBy(string key, string field, long num, TimeSpan? timeout = null);
        /// <summary>
        /// 将key指定的TairHash中一个field的value增加num，num为一个浮点数。如果TairHash不存在则自动新创建一个，如果指定的field不存在，则在加之前插入该field并将其值设置为0。
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field</param>
        /// <param name="num">需要为field的value增加的整数值</param>
        /// <param name="timeout">相对过期过期时间,单位为秒,不传此参数表示不过期</param>
        /// <returns>成功会返回：与num相加后value的值</returns>
        decimal ExHIncrByFloat(string key, string field, decimal num, TimeSpan? timeout = null);
        /// <summary>
        /// 获取key指定的TairHash中field个数，该命令不会触发对过期field的被动淘汰，也不会将其过滤掉，所以结果中可能包含已经过期但还未被删除的field。如果只想返回当前没有过期的field个数，可以在命令中设置NOEXP选项。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="noExpire">只返回当前未过期的field的个数</param>
        /// <returns></returns>
        long ExHLen(string key, bool noExpire = false);
        /// <summary>
        /// 查询key指定的TairHash中是否存在对应的field
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field</param>
        /// <returns></returns>
        bool ExHExists(string key, string field);
        /// <summary>
        /// 获取key指定的TairHash中一个field对应的value的长度。
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash</param>
        /// <param name="field">TairHash中的一个元素，一个TairHash key可以有多个field</param>
        /// <returns></returns>
        long ExHStrLen(string key, string field);
        /// <summary>
        /// 获取key指定的TairHash中所有的field。
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <returns></returns>
        string[] ExHKeys(string key);
        /// <summary>
        /// 获取key指定的TairHash中所有field的值。
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <returns></returns>
        string[] ExHVals(string key);
        /// <summary>
        /// 获取key指定的TairHash中所有field的值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <returns></returns>
        T[] ExHVals<T>(string key);
        /// <summary>
        /// 获取key指定的TairHash中所有field及其value。
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <returns></returns>
        Dictionary<string, string> ExHGetAll(string key);
        /// <summary>
        /// 获取key指定的TairHash中所有field及其value。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <returns></returns>
        Dictionary<string, T> ExHGetAll<T>(string key);
        /// <summary>
        /// 扫描Key指定的TairHash
        /// </summary>
        /// <param name="key">TairHash的Key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="cursor">指定本次扫描的游标</param>
        /// <param name="pattern">用于过滤扫描结果，根据MATCH指定的pattern对待扫描的Key进行正则过滤</param>
        /// <param name="count">用于规定单次扫描field的个数（默认为10）。</param>
        /// <returns></returns>
        ScanResult<KeyValuePair<string, string>> ExHScan(string key, long cursor, string pattern, long count);
        /// <summary>
        /// 删除key指定的TairHash中的一个field，如果TairHash不存在或者field不存在则返回0 ，成功删除返回1。
        /// </summary>
        /// <param name="key">TairHash的key，用于指定作为命令调用对象的TairHash。</param>
        /// <param name="fields">需要删除的field数组</param>
        /// <returns></returns>
        long ExHDel(string key, params string[] fields);

        #endregion
    }
}
