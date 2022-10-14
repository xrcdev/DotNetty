using ServiceStack.Text;
using StackExchange.Redis;
namespace SanTint.Message.MessageCenter.Core.RedisProxy
{
    public class RedisHelper
    {
        public RedisHelper(string ConnectionConfig)
        {
            CreateConnection(ConnectionConfig);
        }

        private ConnectionMultiplexer RedisCon = null;
        private ConfigurationOptions configurationOptions = null;

        private void CreateConnection(string ConnectionConfig)
        {
            configurationOptions = ConfigurationOptions.Parse(ConnectionConfig);

            RedisCon = ConnectionMultiplexer.Connect(configurationOptions);
        }

        public string Prefix { get; set; } = "STV1_";

        #region 基本读写

        /// <summary>
        /// 根据Key获取值，只对string类型有效
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.StringGet(PrefixedKey(key));
        }

        /// <summary>
        /// 根据传入的key获取一条记录的值
        /// </summary>
        /// <typeparam name="T">泛型约束为引用类型</typeparam>
        /// <param name="key"></param>
        /// <returns>当键不存在时，返回NULL</returns>
        public T Get<T>(string key)
        {
            var db = RedisCon.GetDatabase();
            return ToObj<T>(db.StringGet(PrefixedKey(key)));
        }

        /// <summary>
        /// 判断Key在本数据库内是否已被使用(包括各种类型、内置集合等等)
        /// </summary>
        /// <param name="key"></param>
        /// <returns>存在为true 不存在=false</returns>
        public bool ContainsKey(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.KeyExists(PrefixedKey(key));
        }

        /// <summary>
        /// 根据传入的key修改一条记录的值，当key不存在则添加,存在则覆盖。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">键名</param>
        /// <param name="value">键值</param>
        /// <returns></returns>
        public bool Set<T>(string key, T value)
        {
            var db = RedisCon.GetDatabase();
            return db.StringSet(PrefixedKey(key), ToJson(value));
        }

        /// <summary>
        /// 根据传入的key修改一条记录的值，当key不存在则添加,存在则覆盖。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">键名</param>
        /// <param name="value">键值</param>
        /// <param name="Expire">过期时间</param>
        /// <returns></returns>
        public bool Set<T>(string key, T value, DateTime Expire)
        {
            var db = RedisCon.GetDatabase();
            return db.StringSet(PrefixedKey(key), ToJson(value), Expire - DateTime.Now);
        }

        public bool Set<T>(string key, T value, TimeSpan? Expire)
        {
            var db = RedisCon.GetDatabase();
            return db.StringSet(PrefixedKey(key), ToJson(value), Expire);
        }

        /// <summary>
        /// 根据传入的key移除键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.KeyDelete(PrefixedKey(key));
        }

        /// <summary>
        /// 根据指定的Key，将值减去指定值(仅整型有效)
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回减去后的值</returns>
        public long DecrementValue(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.StringDecrement(PrefixedKey(key));
        }

        /// <summary>
        /// 根据指定的Key，将值减去指定值(仅整型有效)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="count">减去的数量</param>
        /// <returns>返回减去后的值</returns>
        public long DecrementValue(string key, int count)
        {
            var db = RedisCon.GetDatabase();
            return db.StringDecrement(PrefixedKey(key), count);
        }

        /// <summary>
        /// 根据指定的Key，将值加1(仅整型有效)
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回加后的值</returns>
        public long IncrementValue(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.StringIncrement(PrefixedKey(key));
        }

        /// <summary>
        /// 根据指定的Key，将值加上指定值(仅整型有效)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="count">递增数</param>
        /// <returns>返回加后的值</returns>
        public long IncrementValue(string key, int count)
        {
            var db = RedisCon.GetDatabase();
            return db.StringIncrement(PrefixedKey(key), count);
        }

        /// <summary>
        /// 根据指定的key设置一项的到期时间（DateTime）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireAt"></param>
        /// <returns></returns>
        public bool ExpireEntryAt(string key, DateTime expireAt)
        {
            var db = RedisCon.GetDatabase();
            return db.KeyExpire(PrefixedKey(key), expireAt);
        }

        /// <summary>
        /// 根据指定的key设置一项的到期时间（DateTime）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireAt"></param>
        /// <returns></returns>
        public bool ExpireEntryAt(string key, TimeSpan expireAt)
        {
            var db = RedisCon.GetDatabase();
            return db.KeyExpire(PrefixedKey(key), expireAt);
        }

        /// <summary>
        /// 获取指定Key的项距离失效点的TimeSpan
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TimeSpan GetTimeToLive(string key)
        {
            var db = RedisCon.GetDatabase();
            TimeSpan? timeSpan = db.KeyTimeToLive(PrefixedKey(key));
            if (timeSpan.HasValue)
            {
                return timeSpan.Value == TimeSpan.MaxValue ? TimeSpan.FromSeconds(-1) : timeSpan.Value;
            }
            return TimeSpan.FromSeconds(-2);
        }

        /// <summary>
        /// 从数据库中查找名称相等的Keys的集合
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public List<string> SearchKeys(string pattern)
        {
            var db = RedisCon.GetDatabase();
            var server = RedisCon.GetServer(configurationOptions.EndPoints[0]);
            return ConvetList(server.Keys(0, PrefixedKey(pattern)));
        }

        #endregion 基本读写

        #region List

        /// <summary>
        /// 将一个值插入到List<T>的最前面  client.LPush
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        public void PrependItemToList<T>(string listId, T value)
        {
            var db = RedisCon.GetDatabase();
            db.ListLeftPush(PrefixedKey(listId), ToJson(value));
        }

        /// <summary>
        ///  将一个值插入到List<T>的最后面   client.RPush
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        public void AddItemToList<T>(string listId, T value)
        {
            var db = RedisCon.GetDatabase();
            db.ListRightPush(PrefixedKey(listId), ToJson(value));
        }

        /// <summary>
        /// 将一个元素存入指定ListId的List<T>的尾部    client.RPush
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        public void PushItemToList<T>(string listId, T value)
        {
            AddItemToList(listId, value);
        }

        public void AddRangeToList<T>(string listId, List<T> values)
        {
            var db = RedisCon.GetDatabase();
            db.ListRightPush(PrefixedKey(listId), ConvertRedisValue(values));
        }

        /// <summary>
        /// 返回List字符串列表
        /// </summary>
        /// <param name="listId"></param>
        /// <returns>当键不存在时，返回NULL</returns>
        public List<string> GetAllItemsFromList(string listId)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList(db.ListRange(PrefixedKey(listId)));
        }

        /// <summary>
        /// 获取指定ListId的内部List<string>的所有值，支持泛型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listId"></param>
        /// <returns>当键不存在时，返回NULL</returns>
        public List<T> GetAllItemsFromList<T>(string listId)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.ListRange(PrefixedKey(listId)));
        }

        /// <summary>
        /// 获取指定ListId的内部List<string>中指定下标范围的数据，支持泛型
        /// </summary>
        /// <param name="listId"></param>
        /// <returns>当键不存在时，返回NULL</returns>
        public List<T> GetRangeFromList<T>(string listId, int startingFrom, int count)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.ListRange(PrefixedKey(listId), startingFrom, startingFrom + count - 1));
        }

        /// <summary>
        /// 移除指定ListId的内部List<string>中第二个参数值相等的那一项
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        /// <returns>返回被删除的元素数量</returns>
        public long RemoveItemFromList<T>(string listId, T value)
        {
            var db = RedisCon.GetDatabase();
            return db.ListRemove(PrefixedKey(listId), ToJson(value));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        /// <param name="count">count > 0: 从头往尾移除值为 value 的元素。count小于0 从尾往头移除值为 value 的元素count = 0: 移除所有值为 value 的元素。</param>
        /// <returns>返回删除元素的个数</returns>
        public long RemoveItemFromList<T>(string listId, T value, int count = 0)
        {
            var db = RedisCon.GetDatabase();
            return db.ListRemove(PrefixedKey(listId), ToJson(value), count);
        }

        /// <summary>
        /// 从指定ListId的List<T>末尾移除一项并返回   client.RPop  返回并弹出指定Key关联的链表中的最后一个元素，即尾部元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listId"></param>
        /// <returns></returns>
        public string PopItemFromList(string listId)
        {
            var db = RedisCon.GetDatabase();
            return db.ListRightPop(PrefixedKey(listId));
        }

        /// <summary>
        /// 从指定ListId的List<T>末尾移除一项并返回   client.RPop  返回并弹出指定Key关联的链表中的最后一个元素，即尾部元素
        /// </summary>
        /// <typeparam name="T">仅限引用类型，弹出值类型请用可空值类型</typeparam>
        /// <param name="listId"></param>
        /// <returns>当键不存在时，返回NULL</returns>
        public T PopItemFromList<T>(string listId)
        {
            var db = RedisCon.GetDatabase();
            return ToObj<T>(db.ListRightPop(PrefixedKey(listId)));
        }

        /// <summary>
        /// 根据ListId，获取内置的List<T>的项数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetListCount(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.ListLength(PrefixedKey(key));
        }

        /// <summary>
        /// 根据ListId和下标获取一项
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="listIndex"></param>
        /// <returns></returns>
        public string GetItemFromList(string listId, int listIndex)
        {
            var db = RedisCon.GetDatabase();
            return db.ListGetByIndex(PrefixedKey(listId), listIndex);
        }

        /// <summary>
        /// 根据ListId和下标获取一项
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="listIndex"></param>
        /// <returns>当键不存在时，返回NULL</returns>
        public T GetItemFromList<T>(string listId, int listIndex)
        {
            var db = RedisCon.GetDatabase();
            return ToObj<T>(db.ListGetByIndex(PrefixedKey(listId), listIndex));
        }

        #endregion List

        #region Set

        /// <summary>
        /// 添加一个项到内部的Set，对应 SADD
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setId"></param>
        /// <param name="item"></param>
        public void AddItemToSet<T>(string setId, T item)
        {
            var db = RedisCon.GetDatabase();
            db.SetAdd(PrefixedKey(setId), ToJson(item));
        }

        /// <summary>
        /// 移除item后，将元素从一个集合移动到另一个集合的开头  对应SMOVE 命令
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fromSetId"></param>
        /// <param name="toSetId"></param>
        /// <param name="item"></param>
        public void MoveBetweenSets<T>(string fromSetId, string toSetId, T item)
        {
            var db = RedisCon.GetDatabase();
            db.SetMove(PrefixedKey(fromSetId), PrefixedKey(toSetId), ToJson(item));
        }

        /// <summary>
        /// 获取指定SetId的内部HashSet<T>的所有值
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        public HashSet<string> GetAllItemsFromSet(string setId)
        {
            var db = RedisCon.GetDatabase();
            return ConvetSet<string>(db.SetMembers(PrefixedKey(setId)));
        }

        /// <summary>
        /// 获取指定SetId的内部HashSet<T>的所有值
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        public HashSet<T> GetAllItemsFromSet<T>(string setId)
        {
            var db = RedisCon.GetDatabase();
            return ConvetSet<T>(db.SetMembers(PrefixedKey(setId)));
        }

        /// <summary>
        ///从指定SetId的内部HashSet<T>中移除与第二个参数值相等的那一项
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void RemoveItemFromSet<T>(string setId, T item)
        {
            var db = RedisCon.GetDatabase();
            db.SetRemove(PrefixedKey(setId), ToJson(item));
        }

        /// <summary>
        /// 从指定setId的集合中获取随机项
        /// </summary>
        /// <param name="setId"></param>
        /// <returns></returns>
        public T GetRandomItemFromSet<T>(string setId) where T : class
        {
            var db = RedisCon.GetDatabase();
            return ToObj<T>(db.SetRandomMember(PrefixedKey(setId)));
        }

        /// <summary>
        /// 根据SetId，获取内置的HashSet<T>的项数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetSetCount(string setId)
        {
            var db = RedisCon.GetDatabase();
            return db.SetLength(PrefixedKey(setId));
        }

        /// <summary>
        /// 判断指定SetId的HashSet<T>中是否包含指定的value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setId"></param>
        /// <returns></returns>
        public bool SetContainsItem<T>(string setId, T item)
        {
            var db = RedisCon.GetDatabase();
            return db.SetContains(PrefixedKey(setId), ToJson(item));
        }

        /// <summary>
        /// 根据Key设置一个值，仅仅当Key不存在时有效，如Key已存在则不修改(只支持字符串)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetEntryIfNotExists(string key, string value)
        {
            var db = RedisCon.GetDatabase();
            return db.StringSet(PrefixedKey(key), value, null, When.NotExists);
        }

        #endregion Set

        #region SortedSet

        /// <summary>
        /// 添加一个项到内部的排序List<T>，其中重载方法多了个score：排序值。优先按照score从小->大排序，否则按值小到大排序。支持泛型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setId"></param>
        /// <param name="value"></param>
        public void AddItemToSortedSet<T>(string setId, T value, double score)
        {
            var db = RedisCon.GetDatabase();
            db.SortedSetAdd(PrefixedKey(setId), ToJson(value), score);
        }

        public void AddItemToSortedSet<T>(string setId, Dictionary<T, double> dic)
        {
            var db = RedisCon.GetDatabase();
            db.SortedSetAdd(PrefixedKey(setId), ConvertSortedSetEntry(dic));
        }

        /// <summary>
        /// 获取已排序集合的项的数目
        /// </summary>
        /// <param name="setId"></param>
        /// <returns></returns>
        public long GetSortedSetCount(string setId)
        {
            var db = RedisCon.GetDatabase();
            return db.SortedSetLength(PrefixedKey(setId));
        }

        /// <summary>
        /// 获取已排序集合的项的数目，支持下标以及score筛选
        /// </summary>
        /// <param name="setId"></param>
        /// <param name="min">最低分</param>
        /// <param name="max">最高分</param>
        /// <returns></returns>
        public long GetSortedSetCount(string setId, double min, double max)
        {
            var db = RedisCon.GetDatabase();
            return db.SortedSetLength(PrefixedKey(setId), min, max);
        }

        public List<string> GetAllItemsFromSortedSet(string setId)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList(db.SortedSetRangeByRank(PrefixedKey(setId)));
        }

        /// <summary>
        ///  获取指定ListId的内部已排序List<T>的所有值，支持泛型
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        public List<T> GetAllItemsFromSortedSet<T>(string setId)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.SortedSetRangeByRank(PrefixedKey(setId)));
        }

        /// <summary>
        /// 获取指定ListId的内部已排序List<string>的所有值，倒序
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        public List<string> GetAllItemsFromSortedSetDesc(string setId, long start = 0, long stop = -1, Order order = Order.Ascending)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList(db.SortedSetRangeByRank(PrefixedKey(setId), start, stop, Order.Descending));
        }

        /// <summary>
        ///  获取指定ListId的内部已排序List<T>的所有值，倒序，支持泛型
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        public List<T> GetAllItemsFromSortedSetDesc<T>(string setId, long start = 0, long stop = -1, Order order = Order.Ascending)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.SortedSetRangeByRank(PrefixedKey(setId), start, stop, Order.Descending));
        }

        /// <summary>
        /// 获取指定SetId的内部List<T>中指定下标范围的数据
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="startingFrom"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<T> GetRangeFromSortedSet<T>(string setId, double start, double stop)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.SortedSetRangeByScore(PrefixedKey(setId), start, stop));
        }

        /// <summary>
        /// 获取指定SetId的内部List<T>中指定下标范围的数据，倒序
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="startingFrom"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<T> GetRangeFromSortedSetDesc<T>(string setId, double start, double stop)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.SortedSetRangeByScore(PrefixedKey(setId), start, stop, Exclude.None, Order.Descending));
        }

        /// <summary>
        /// 根据List和值，获取内置的排序后的List<string>的下标
        /// </summary>
        /// <param name="setId"></param>
        /// <param name="value"></param>
        /// <returns>如果没有找到该项，返回-1</returns>
        public long GetItemIndexInSortedSet(string setId, string value, Order order = Order.Descending)
        {
            var db = RedisCon.GetDatabase();
            long? index = db.SortedSetRank(PrefixedKey(setId), value, order);
            return index.HasValue ? index.Value : -1;
        }

        /// <summary>
        /// 根据List和值，获取内置的排序后的List<string>的下标  倒序
        /// </summary>
        /// <param name="setId"></param>
        /// <param name="value"></param>
        /// <returns>如果没有找到该项，返回-1</returns>
        public long GetItemIndexInSortedSetDesc(string setId, string value)
        {
            return GetItemIndexInSortedSet(setId, value, Order.Descending);
        }

        /// <summary>
        /// 获取指定SetId的内部List<string>中按照score由高->低排序后的分值范围的数据
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="startingFrom"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<T> GetRangeFromSortedSetByHighestScore<T>(string setId, double start, double stop)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.SortedSetRangeByScore(PrefixedKey(setId), start, stop, Exclude.None, Order.Descending));
        }

        /// <summary>
        /// 获取指定SetId的内部List<string>中按照score由高->低排序后的分值范围的数据
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="startingFrom"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<T> GetRangeFromSortedSetByScore<T>(string setId, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = Exclude.None,
            Order order = Order.Ascending, long skip = 0, long take = -1)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.SortedSetRangeByScore(PrefixedKey(setId), start, stop, exclude, order, skip, take));
        }

        public long RemoveRangeFromSortedSetByScore(string setId, double fromScore, double toScore)
        {
            var db = RedisCon.GetDatabase();
            return db.SortedSetRemoveRangeByScore(PrefixedKey(setId), fromScore, toScore);
        }

        public List<T> GetRangeFromSortedSet<T>(string setId, long start = 0, long stop = -1, Order order = Order.Ascending)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.SortedSetRangeByRank(PrefixedKey(setId), start, stop, order));
        }

        /// <summary>
        /// 根据传入的ListId和值获取内置List<string>项的score
        /// </summary>
        /// <param name="setId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public double GetItemScoreInSortedSet(string setId, string value)
        {
            var db = RedisCon.GetDatabase();
            double? score = db.SortedSetScore(PrefixedKey(setId), value);
            return score.HasValue ? score.Value : 0D;
        }

        /// <summary>
        ///判断SortedSet是否包含一个键
        /// </summary>
        /// <param name="setId"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SortedSetContainsItem(string setId, string item)
        {
            var db = RedisCon.GetDatabase();
            long? index = db.SortedSetRank(PrefixedKey(setId), item);
            return index.HasValue ? true : false;
        }

        /// <summary>
        /// 判断SortedSet是否包含一个键
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setId"></param>
        /// <returns></returns>
        public bool SortedSetContainsItem<T>(string setId, T item)
        {
            return SortedSetContainsItem(setId, ToJson(item));
        }

        /// <summary>
        /// 从指定SetId的内部List<T>中移除与第二个参数值相等的那一项
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        /// <param name="noOfMatches"></param>
        /// <returns></returns>
        public bool RemoveItemFromSortedSet(string setId, string value)
        {
            var db = RedisCon.GetDatabase();
            return db.SortedSetRemove(PrefixedKey(setId), value);
        }

        /// <summary>
        ///从指定SetId的内部List<T>中移除与第二个参数值相等的那一项
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool RemoveItemFromSortedSet<T>(string setId, T value)
        {
            return RemoveItemFromSortedSet(setId, ToJson(value));
        }

        /// <summary>
        /// 升序队列，弹出第一个，并删除 限制引用类型，值类型，须调用PopValueWithLowestScoreFromSortedSet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setId"></param>
        /// <returns>键不存在时候，弹出NULL</returns>
        public T PopItemWithLowestScoreFromSortedSet<T>(string setId)
        {
            var db = RedisCon.GetDatabase();
            RedisValue[] rs = db.SortedSetRangeByRank(PrefixedKey(setId), 0, 1);
            if (rs.Length == 0)
            {
                return default(T);
            }
            db.SortedSetRemove(PrefixedKey(setId), rs[0]);
            return ToObj<T>(rs[0]);
        }

        public T PopItemWithHighestScoreFromSortedSet<T>(string setId)
        {
            var db = RedisCon.GetDatabase();
            RedisValue[] rs = db.SortedSetRangeByRank(PrefixedKey(setId), 0, 1, Order.Descending);
            if (rs.Length == 0)
            {
                return default(T);
            }
            db.SortedSetRemove(PrefixedKey(setId), rs[0]);
            return ToObj<T>(rs[0]);
        }

        #endregion SortedSet

        #region HashSet

        /// <summary>
        /// 设置一个键值对入Hash表，如果哈希表的key存在则覆盖
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="hashId">哈希键名</param>
        /// <param name="value">哈希键值</param>
        /// <returns>1表示新的Field被设置了新值，0表示Field已经存在，用新值覆盖原有值。 </returns>
        public bool SetEntryInHash(string key, string hashId, string value)
        {
            var db = RedisCon.GetDatabase();
            return db.HashSet(PrefixedKey(key), hashId, value);
        }

        /// <summary>
        /// 设置一个键值对入Hash表，如果哈希表的key存在则覆盖
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">键名</param>
        /// <param name="hashId">哈希键名</param>
        /// <param name="value">哈希键值</param>
        /// <returns>True表示新的Field被设置了新值，False表示Field已经存在，用新值覆盖原有值。 </returns>
        public bool SetEntryInHash<T>(string key, string hashId, T value)
        {
            var db = RedisCon.GetDatabase();
            return db.HashSet(PrefixedKey(key), hashId, ToJson(value));
        }

        //public    void SetRangeInHash(string key, HashEntry[] keyValuePairs)
        //{
        //    var db = RedisCon.GetDatabase();
        //    db.HashSet(PrefixedKey(key), keyValuePairs);
        //}
        public void SetRangeInHash<T>(string key, Dictionary<string, T> keyValuePairs)
        {
            var db = RedisCon.GetDatabase();
            db.HashSet(PrefixedKey(key), ConvertHashEntry(keyValuePairs));
        }

        /// <summary>
        /// 当哈希表的key未被使用时，设置一个键值对入Hash表
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <param name="value"></param>
        /// <returns>1表示新的Field被设置了新值，0表示Key或Field已经存在，该命令没有进行任何操作。 </returns>
        public bool SetEntryInHashIfNotExists(string key, string hashId, string value)
        {
            var db = RedisCon.GetDatabase();
            return db.HashSet(PrefixedKey(key), hashId, value, When.NotExists);
        }

        /// <summary>
        /// 当哈希表的key未被使用时，设置一个键值对如Hash表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <param name="value"></param>
        /// <returns>1表示新的Field被设置了新值，0表示Key或Field已经存在，该命令没有进行任何操作。 </returns>
        public bool SetEntryInHashIfNotExists<T>(string key, string hashId, T value)
        {
            var db = RedisCon.GetDatabase();
            return db.HashSet(PrefixedKey(key), hashId, ToJson(value), When.NotExists);
        }

        /// <summary>
        /// 根据key获取该Hash下的所有值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string> GetHashValues(string key)
        {
            var db = RedisCon.GetDatabase();
            RedisValue[] rs = db.HashValues(PrefixedKey(key));
            return ConvetList<string>(rs);
        }

        /// <summary>
        /// 根据HashId获取该HashId下的所有值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<T> GetHashValues<T>(string key)
        {
            var db = RedisCon.GetDatabase();
            RedisValue[] rs = db.HashValues(PrefixedKey(key));
            return ConvetList<T>(rs);
        }

        /// <summary>
        /// 根据HashId和Hash表的Key获取多个值(支持多个key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <returns></returns>
        public List<string> GetValuesFromHash(string key, params string[] hashIds)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<string>(db.HashGet(PrefixedKey(key), ConvertRedisValue(hashIds)));
        }

        /// <summary>
        /// 根据HashId和Hash表的Key获取多个值(支持多个key)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <returns></returns>
        public List<T> GetValuesFromHash<T>(string key, params string[] hashIds)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList<T>(db.HashGet(PrefixedKey(key), ConvertRedisValue(hashIds)));
        }

        /// <summary>
        /// 获取键下某个HashID的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <returns></returns>
        public string GetValueFromHash(string key, string hashId)
        {
            var db = RedisCon.GetDatabase();
            return db.HashGet(PrefixedKey(key), hashId);
        }

        /// <summary>
        ///  获取键下某个HashID的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <returns>当字段不存在或者 key 不存在时返回NULL</returns>
        public T GetValueFromHash<T>(string key, string hashId)
        {
            var db = RedisCon.GetDatabase();
            return ToObj<T>(db.HashGet(PrefixedKey(key), hashId));
        }

        /// <summary>
        /// 获取键指定下的所有hash Key
        /// </summary>
        /// <param name="hashId"></param>
        /// <returns></returns>
        public List<string> GetHashKeys(string key)
        {
            var db = RedisCon.GetDatabase();
            return ConvetList((db.HashKeys(PrefixedKey(key))));
        }

        /// <summary>
        /// 获取指定键名下的所有Key数量
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns></returns>
        public long GetHashCount(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.HashLength(PrefixedKey(key));
        }

        /// <summary>
        /// 判断指定键的哈希表中是否包含指定的hashId
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <returns></returns>
        public bool HashContainsEntry(string key, string hashId)
        {
            var db = RedisCon.GetDatabase();
            return db.HashExists(PrefixedKey(key), hashId);
        }

        /// <summary>
        /// 根据指定键获取所有的hash键值对
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            var db = RedisCon.GetDatabase();
            return ConvertDictionary<string>(db.HashGetAll(PrefixedKey(key)));
        }

        /// <summary>
        /// 根据指定键获取所有的hash键值对
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<string, T> GetAllEntriesFromHash<T>(string key)
        {
            var db = RedisCon.GetDatabase();
            return ConvertDictionary<T>(db.HashGetAll(PrefixedKey(key)));
        }

        /// <summary>
        /// 移除hash中的某值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public bool RemoveEntryFromHash(string key, string hashId)
        {
            var db = RedisCon.GetDatabase();
            return db.HashDelete(PrefixedKey(key), hashId);
        }

        /// <summary>
        /// 将指定HashId的哈希表中的值加上指定值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hashId"></param>
        /// <param name="incrementBy"></param>
        /// <returns></returns>
        public long IncrementValueInHash(string key, string hashId, int incrementBy)
        {
            var db = RedisCon.GetDatabase();
            return db.HashIncrement(PrefixedKey(key), hashId, incrementBy);
        }

        #endregion HashSet

        #region 辅助方法

        /// <summary>
        /// 根据Key获取当前存储的值是什么类型（None = 0,String = 1,List = 2,Set = 3,SortedSet = 4,Hash = 5,）
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public RedisType GetEntryType(string key)
        {
            var db = RedisCon.GetDatabase();
            return db.KeyType(PrefixedKey(key));
        }

        /// <summary>
        /// 计数方法
        /// </summary>
        /// <param name="CacheKey">键名</param>
        /// <param name="ExpireEntryIn">有效期（单位：秒）</param>
        /// <returns></returns>
        public long GetRepeatNum(string key, int ExpireEntryIn)
        {
            long num = IncrementValue(key);
            if (num == 1 || GetTimeToLive(key).Seconds < 0)
            {
                ExpireEntryAt(key, new TimeSpan(0, 0, ExpireEntryIn));
            }
            return num;
        }

        private string PrefixedKey(string key)
        {
            return string.Concat(Prefix, key);
        }

        private string ToJson<T>(T value)
        {
            if (value is string || typeof(T).IsPrimitive)
            {
                return value.ToString();
            }
            else
            {
                return JsonSerializer.SerializeToString(value);
            }
        }

        private T ToObj<T>(RedisValue value)
        {
            if (value.IsNull)
            {
                return default(T);
            }
            else
            {
                if (typeof(T).IsPrimitive)
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                else
                {
                    return JsonSerializer.DeserializeFromString<T>(value);
                }
            }
        }

        private List<string> ConvetList(RedisValue[] values)
        {
            if (values.Length == 0)
            {
                return null;
            }
            List<string> result = new List<string>();
            foreach (var item in values)
            {
                result.Add(item);
            }
            return result;
        }

        private List<string> ConvetList(IEnumerable<RedisKey> values)
        {
            List<string> result = new List<string>();
            foreach (var item in values)
            {
                result.Add(item);
            }
            return result;
        }

        private List<T> ConvetList<T>(RedisValue[] values)
        {
            if (values.Length == 0)
            {
                return null;
            }
            List<T> result = new List<T>();
            foreach (var item in values)
            {
                var model = ToObj<T>(item);
                result.Add(model);
            }
            return result;
        }

        private Dictionary<string, T> ConvertDictionary<T>(HashEntry[] values)
        {
            if (values.Length == 0)
            {
                return null;
            }
            Dictionary<string, T> result = new Dictionary<string, T>();
            foreach (var item in values)
            {
                result.Add(item.Name, ToObj<T>(item.Value));
            }
            return result;
        }

        private HashSet<string> ConvetSet(RedisValue[] values)
        {
            if (values.Length == 0)
            {
                return null;
            }
            HashSet<string> result = new HashSet<string>();
            foreach (var item in values)
            {
                result.Add(item.ToString());
            }
            return result;
        }

        private HashSet<T> ConvetSet<T>(RedisValue[] values)
        {
            if (values.Length == 0)
            {
                return null;
            }
            HashSet<T> result = new HashSet<T>();
            foreach (var item in values)
            {
                result.Add(ToObj<T>(item));
            }
            return result;
        }

        private RedisValue[] ConvertRedisValue(string[] values)
        {
            RedisValue[] result = new RedisValue[values.Length];
            if (values.Length > 0)
            {
                int i = 0;
                foreach (var item in values)
                {
                    result[i] = item;
                    i++;
                }
            }
            return result;
        }

        private RedisValue[] ConvertRedisValue(int[] values)
        {
            RedisValue[] result = new RedisValue[values.Length];
            if (values.Length > 0)
            {
                int i = 0;
                foreach (var item in values)
                {
                    result[i] = item;
                    i++;
                }
            }
            return result;
        }

        private RedisValue[] ConvertRedisValue<T>(List<T> list)
        {
            RedisValue[] result = new RedisValue[list.Count];
            if (list.Count > 0)
            {
                int i = 0;
                foreach (var item in list)
                {
                    result[i] = ToJson(item);
                    i++;
                }
            }
            return result;
        }

        private HashEntry[] ConvertHashEntry<T>(Dictionary<string, T> dic)
        {
            HashEntry[] result = new HashEntry[dic.Count];
            if (dic.Count > 0)
            {
                int i = 0;
                foreach (KeyValuePair<string, T> item in dic)
                {
                    result[i] = new HashEntry(item.Key, ToJson(item.Value));
                    i++;
                }
            }
            return result;
        }

        private SortedSetEntry[] ConvertSortedSetEntry<T>(Dictionary<T, double> dic)
        {
            SortedSetEntry[] result = new SortedSetEntry[dic.Count];
            if (dic.Count > 0)
            {
                int i = 0;
                foreach (KeyValuePair<T, double> item in dic)
                {
                    result[i] = new SortedSetEntry(ToJson(item.Key), item.Value);
                    i++;
                }
            }
            return result;
        }

        #endregion 辅助方法
    }
}