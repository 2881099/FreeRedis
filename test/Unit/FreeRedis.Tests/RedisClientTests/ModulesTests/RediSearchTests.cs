using FreeRedis.RediSearch;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class RediSearchTests : TestBase
    {
		
		protected static ConnectionStringBuilder Connection = new ConnectionStringBuilder()
        {
            Host = "8.154.26.11",
            MaxPoolSize = 10,
            Protocol = RedisProtocol.RESP2,
            ClientName = "FreeRedis",
            //FtDialect = 4,
            FtLanguage = "chinese"
        };
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            var r = new RedisClient(Connection);
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });
        public static RedisClient cli => _cliLazy.Value;

        [FtDocument("index_post", Prefix = "blog:post:")]
        class TestDoc
        {
            [FtKey]
            public int Id { get; set; }

            [FtTextField("title", Weight = 5.0)]
            public string Title { get; set; }

            [FtTextField("category")]
            public string Category { get; set; }

            [FtTextField("content", Weight = 1.0, NoIndex = true)]
            public string Content { get; set; }

            [FtTagField("tags")]
            public string Tags { get; set; }

            [FtNumericField("views")]
            public int Views { get; set; }
        }

        [FtDocument("index_post100", Prefix = "blog:post:")]
        class TagMapArrayIndex
        {
            [FtKey]
            public int Id { get; set; }

            [FtTextField("title", Weight = 5.0)]
            public string Title { get; set; }

            [FtTextField("category")]
            public string Category { get; set; }

            [FtTextField("content", Weight = 1.0, NoIndex = true)]
            public string Content { get; set; }

            [FtTagField("tags")]
            public string[] Tags { get; set; }

            [FtNumericField("views")]
            public int Views { get; set; }
        }
        [Fact]
        public void TagMapArray()
        {
            var repo = cli.FtDocumentRepository<TagMapArrayIndex>();

            try
            {
                repo.DropIndex();
            }
            catch { }
            repo.CreateIndex();

            repo.Save(new TagMapArrayIndex { Id = 1, Title = "测试标题1 word", Category = "一级分类", Content = "测试内容1suffix", Tags = ["作者1","作者2"], Views = 101 });
            repo.Save(new TagMapArrayIndex { Id = 2, Title = "prefix测试标题2", Category = "二级分类", Content = "测试infix内容2", Tags = ["作者2","作者3"], Views = 201 });
            repo.Save(new TagMapArrayIndex { Id = 3, Title = "测试标题3 word", Category = "一级分类", Content = "测试word内容3", Tags = ["作者2","作者5"], Views = 301 });

            repo.Delete(1, 2, 3);

            repo.Save(new[]{
                new TagMapArrayIndex { Id = 1, Title = "测试标题1 word", Category = "一级分类", Content = "测试内容1suffix", Tags = ["作者1","作者2"], Views = 101 },
                new TagMapArrayIndex { Id = 2, Title = "prefix测试标题2", Category = "二级分类", Content = "测试infix内容2", Tags = ["作者2","作者3"], Views = 201 },
                new TagMapArrayIndex { Id = 3, Title = "测试标题3 word", Category = "一级分类", Content = "测试word内容3", Tags = ["作者2","作者5"], Views = 301 }
            });

            var list = repo.Search(a => a.Tags.Contains("作者1")).ToList();
        }

        [Fact]
        public void FtDocumentRepository()
        {
            var connStr = Connection.ToString();

            var repo = cli.FtDocumentRepository<TestDoc>();

            try
            {
                repo.DropIndex();
            }
            catch { }
            repo.CreateIndex();

            repo.Save(new TestDoc { Id = 1, Title = "测试标题1 word", Category = "一级分类", Content = "测试内容1suffix", Tags = "作者1,作者2", Views = 101 });
            repo.Save(new TestDoc { Id = 2, Title = "prefix测试标题2", Category = "二级分类", Content = "测试infix内容2", Tags = "作者2,作者3", Views = 201 });
            repo.Save(new TestDoc { Id = 3, Title = "测试标题3 word", Category = "一级分类", Content = "测试word内容3", Tags = "作者2,作者5", Views = 301 });

            repo.Delete(1, 2, 3);

            repo.Save(new[]{
                new TestDoc { Id = 1, Title = "测试标题1 word", Category = "一级分类", Content = "测试内容1suffix", Tags = "作者1,作者2", Views = 101 },
                new TestDoc { Id = 2, Title = "prefix测试标题2", Category = "二级分类", Content = "测试infix内容2", Tags = "作者2,作者3", Views = 201 },
                new TestDoc { Id = 3, Title = "测试标题3 word", Category = "一级分类", Content = "测试word内容3", Tags = "作者2,作者5", Views = 301 }
            });

            var list = repo.Search("*").InFields(a => new { a.Title }).ToList();
            list = repo.Search("*").Return(a => new { a.Title, a.Tags }).ToList();
            list = repo.Search("*").Return(a => new { tit1 = a.Title, tgs1 = a.Tags, a.Title, a.Tags }).ToList();

            list = repo.Search(a => a.Title == "word" || a.Views > 100).Filter(a => a.Views, 1, 1000).ToList();
            list = repo.Search(a => a.Title.Contains("word") || a.Views > 100).Filter(a => a.Views, 1, 1000).ToList();
            list = repo.Search(a => a.Tags == "作者1").Filter(a => a.Views, 1, 1000).ToList();
            list = repo.Search("word").ToList();
            list = repo.Search("@title:word").ToList();
            list = repo.Search("prefix*").ToList();
            list = repo.Search("@title:prefix*").ToList();
            list = repo.Search("*suffix").ToList();
            list = repo.Search("*infix*").ToList();
            list = repo.Search("%word%").ToList();

            list = repo.Search("@views:[200 300]").ToList();
            list = repo.Search("@views:[-inf 2000]").SortBy(a => a.Views).Limit(0, 5).ToList();
            list = repo.Search("@views:[(200 (300]").ToList();
            list = repo.Search("@views>=200").Dialect(4).ToList();
            list = repo.Search("@views:[200 +inf]").ToList();
            list = repo.Search("@views<=300").Dialect(4).ToList();
            list = repo.Search("@views:[-inf 300]").ToList();
            list = repo.Search("@views==200").Dialect(4).ToList();
            list = repo.Search("@views:[200 200]").ToList();
            list = repo.Search("@views!=200").Dialect(4).ToList();
            list = repo.Search("-@views:[200 200]").ToList();
            list = repo.Search("@views==200 | @views==300").Dialect(4).ToList();
            list = repo.Search("*").Filter("views", 200, 300).Dialect(4).ToList();


            list = repo.Search("word").ToList();
            list = repo.Search("@title:word").ToList();
            list = repo.Search("prefix*").ToList();
            list = repo.Search("@title:prefix*").ToList();
            list = repo.Search("*suffix").ToList();
            list = repo.Search("*infix*").ToList();
            list = repo.Search("%word%").ToList();

            list = repo.Search("@views:[200 300]").ToList();
            list = repo.Search("@views:[-inf 2000]").SortBy(a => a.Views).Limit(0, 5).ToList();
            list = repo.Search("@views:[(200 (300]").ToList();
            list = repo.Search("@views>=200").Dialect(4).ToList();
            list = repo.Search("@views:[200 +inf]").ToList();
            list = repo.Search("@views<=300").Dialect(4).ToList();
            list = repo.Search("@views:[-inf 300]").ToList();
            list = repo.Search("@views==200").Dialect(4).ToList();
            list = repo.Search("@views:[200 200]").ToList();
            list = repo.Search("@views!=200").Dialect(4).ToList();
            list = repo.Search("-@views:[200 200]").ToList();
            list = repo.Search("@views==200 | @views==300").Dialect(4).ToList();
            list = repo.Search("*").Filter("views", 200, 300).Dialect(4).ToList();
        }

        [Fact]
        public void FtSearch()
        {
            var rt = cli.FtSearch("index_contacts", "(@email:*wjx\\.cn*) (@companyid==10096)")
                .Limit(0, 10)
                .Dialect(4)
                .Execute();

            var idxName = Guid.NewGuid().ToString();
            cli.FtCreate(idxName)
                .On(IndexDataType.Hash)
                .Prefix("blog:post:")
                .AddTextField("title", weight: 5.0)
                .AddTextField("content")
                .AddTagField("author")
                .AddNumericField("created_date", sortable: true)
                .AddNumericField("views")
                .Execute();

            cli.HSet("blog:post:1", "title", "测试标题1 word", "content", "测试内容1suffix", "author", "作者1,作者2", "created_date", "10000", "views", 10);
            cli.HSet("blog:post:2", "title", "prefix测试标题2", "content", "测试infix内容2", "author", "作者2,作者3", "created_date", "10001", "views", 201);
            cli.HSet("blog:post:3", "title", "测试标题3 word", "content", "测试word内容3", "author", "作者2,作者5", "created_date", "10002", "views", 301);

            var list = cli.FtSearch(idxName, "word").Execute();
            list = cli.FtSearch(idxName, "@title:word").Execute();
            list = cli.FtSearch(idxName, "prefix*").Execute();
            list = cli.FtSearch(idxName, "@title:prefix*").Execute();
            list = cli.FtSearch(idxName, "*suffix").Execute();
            list = cli.FtSearch(idxName, "*infix*").Execute();
            list = cli.FtSearch(idxName, "%word%").Execute();


            list = cli.FtSearch(idxName, "@views:[200 300]").Execute();
            list = cli.FtSearch(idxName, "@views:[-inf 2000]").SortBy("views").Limit(0, 5).Execute();
            list = cli.FtSearch(idxName, "@views:[(200 (300]").Execute();
            list = cli.FtSearch(idxName, "@views>=200").Dialect(4).Execute();
            list = cli.FtSearch(idxName, "@views:[200 +inf]").Execute();
            list = cli.FtSearch(idxName, "@views<=300").Dialect(4).Execute();
            list = cli.FtSearch(idxName, "@views:[-inf 300]").Execute();
            list = cli.FtSearch(idxName, "@views==200").Dialect(4).Execute();
            list = cli.FtSearch(idxName, "@views:[200 200]").Execute();
            list = cli.FtSearch(idxName, "@views!=200").Dialect(4).Execute();
            list = cli.FtSearch(idxName, "-@views:[200 200]").Execute();
            list = cli.FtSearch(idxName, "@views==200 | @views==300").Dialect(4).Execute();
            list = cli.FtSearch(idxName, "*").Filter("views", 200, 300).Dialect(4).Execute();
        }


        [Fact]
        public void FtAggregate()
        {
            var idxName = Guid.NewGuid().ToString();
            cli.FtCreate(idxName)
                .On(IndexDataType.Hash)
                .Prefix("blog:post:")
                .AddTextField("title", weight: 5.0)
                .AddTextField("content")
                .AddTagField("author")
                .AddNumericField("created_date", sortable: true)
                .AddNumericField("views")
                .Execute();

            cli.HSet("blog:post:1", "title", "测试标题1 word", "content", "测试内容1suffix", "author", "作者1,作者2", "created_date", "10000", "views", 10);
            cli.HSet("blog:post:2", "title", "prefix测试标题2", "content", "测试infix内容2", "author", "作者2,作者3", "created_date", "10001", "views", 201);
            cli.HSet("blog:post:3", "title", "测试标题3 word", "content", "测试word内容3", "author", "作者2,作者5", "created_date", "10002", "views", 301);

            var list = cli.FtAggregate(idxName, "word").Load("title", "author").Execute();
            list = cli.FtAggregate(idxName, "@title:word").GroupBy("@title", "@views").Execute();
            list = cli.FtAggregate(idxName, "*").GroupBy(["@title", "@views"], new AggregateReduce { Function = "SUM", Arguments = ["@views"], Alias = "sum1" }).Execute();
            list = cli.FtAggregate(idxName, "*").Load("views").Apply("@views<200", "view_category").GroupBy(["@title"], new AggregateReduce { Function = "SUM", Arguments = ["@view_category"], Alias = "sum1" }).Execute();
        }

        [Fact]
		public void FtCreate()
		{
            cli.FtCreate("idx1")
                .On(IndexDataType.Hash)
                .Prefix("blog:post:")
                .AddTextField("title", weight: 5.0)
                .AddTextField("content")
                .AddTagField("author")
                .AddNumericField("created_date", sortable: true)
                .AddNumericField("views")
                .Execute();

            cli.FtCreate("idx2")
                .On(IndexDataType.Hash)
                .Prefix("book:details:")
                .AddTextField("title")
                .AddTagField("categories", separator: ";")
                .Execute();

            cli.FtCreate("idx3")
                .On(IndexDataType.Hash)
                .Prefix("blog:post:")
                .AddTextField("sku", alias: "sku_text")
                .AddTagField("sku", alias: "sku_tag", sortable: true)
                .Execute();

            cli.FtCreate("idx4")
                .On(IndexDataType.Hash)
                .Prefix("author:details:", "book:details:")
                .AddTagField("author_id", sortable: true)
                .AddTagField("author_ids")
                .AddTextField("title")
                .AddTextField("name")
                .Execute();

            cli.FtCreate("idx5")
                .On(IndexDataType.Hash)
                .Prefix("author:details")
                .Filter("startswith(@name, 'G')")
                .AddTextField("name")
                .Execute();

            cli.FtCreate("idx6")
                .On(IndexDataType.Hash)
                .Prefix("book:details")
                .Filter("@subtitle != ''")
                .AddTextField("title")
                .Execute();

            cli.FtCreate("idx7")
                .On(IndexDataType.Json)
                .Prefix("book:details")
                .Filter("@subtitle != ''")
                .AddTextField("$.title", alias: "title")
                .AddTagField("$.categories", alias: "categories")
                .Execute();
        }


        [Fact]
        public void TestContactsUserIndex()
        {
            var repo = cli.FtDocumentRepository<ContactsUserIndex>();
            //FT.SEARCH index_contacts "(@email:*wjx\\.cn*) (@companyid==10096)" dialect 4
            var rt = repo.Search(@"(@email:*wjx\.cn*) (@companyid==10096)").Dialect(4).ToList();

            var rt2 = repo.Search(a => a.UEmail.Contains("wjx.cn") && a.CompanyId == 10096).Dialect(4).ToList();
        }

        [FtDocument("index_contacts", Prefix = "contactsuser:")]
        public class ContactsUserIndex
        {
            /// <summary>
            /// 用户唯一编号
            /// </summary>

            [FtKey]
            public long Id { get; set; }

            /// <summary>
            /// 用户编号
            /// </summary>
            [FtTextField("userid")]
            public string UserId { get; set; }
            /// <summary>
            /// 用户姓名
            /// </summary>
            [FtTextField("name")]
            public string Name { get; set; }
            /// <summary>
            /// 用户所属部门
            /// </summary>
            [FtTagField("department")]
            public string Department { get; set; }

            /// <summary>
            /// 企业编号
            /// </summary>
            [FtNumericField("companyid")]
            public int CompanyId { get; set; }

            /// <summary>
            /// 用户自定义标签
            /// </summary>
            [FtTagField("utags")]
            public string UTags { get; set; }
            /// <summary>
            /// 添加时间
            /// </summary>
            [FtNumericField("addtime", Sortable = true)]
            public long AddTime { get; set; }
            /// <summary>
            /// 更新时间
            /// </summary>
            [FtNumericField("updatetime")]
            public long UpdateTime { get; set; }

            /// <summary>
            /// 手机号
            /// </summary>
            [FtTextField("umobile")]
            public string UMobile { get; set; }
            /// <summary>
            /// 邮箱
            /// </summary>
            [FtTextField("uemail")]
            public string UEmail { get; set; }
            /// <summary>
            /// 昵称
            /// </summary>
            [FtTextField("unickname")]
            public string UNickname { get; set; }
            /// <summary>
            /// 生日
            /// </summary>
            [FtNumericField("ubirthday")]
            public long UBirthday { get; set; }

            [FtNumericField("lastmsgtime")]
            public long LastMsgTime { get; set; }

            [FtNumericField("lastjointime")]
            public long LastJoinTime { get; set; }

            /// <summary>
            /// 用户信息标识位
            /// </summary>
            [FtNumericField("uinfomap")]
            public long UInfoMap { get; set; }
        }
    }

}
