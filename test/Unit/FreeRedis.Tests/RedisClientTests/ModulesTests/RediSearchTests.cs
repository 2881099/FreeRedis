using FreeRedis.RediSearch;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Xml.Linq;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class RediSearchTests : TestBase
    {
		
		protected static ConnectionStringBuilder Connection = new ConnectionStringBuilder()
        {
            Host = "8.154.26.119",
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

            list = repo.Search(a => a.Title == "word").Filter(a => a.Views, 1, 1000).ToList();
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

            var list = cli.FtAggregate(idxName, "word").Execute();
            list = cli.FtAggregate(idxName, "@title:word").Execute();
            list = cli.FtAggregate(idxName, "prefix*").Execute();
            list = cli.FtAggregate(idxName, "@title:prefix*").Execute();
            list = cli.FtAggregate(idxName, "*suffix").Execute();
            list = cli.FtAggregate(idxName, "*infix*").Execute();
            list = cli.FtAggregate(idxName, "%word%").Execute();


            list = cli.FtAggregate(idxName, "@views:[200 300]").Execute();
            list = cli.FtAggregate(idxName, "@views:[-inf 2000]").SortBy("views").Limit(0, 5).Execute();
            list = cli.FtAggregate(idxName, "@views:[(200 (300]").Execute();
            list = cli.FtAggregate(idxName, "@views>=200").Dialect(4).Execute();
            list = cli.FtAggregate(idxName, "@views:[200 +inf]").Execute();
            list = cli.FtAggregate(idxName, "@views<=300").Dialect(4).Execute();
            list = cli.FtAggregate(idxName, "@views:[-inf 300]").Execute();
            list = cli.FtAggregate(idxName, "@views==200").Dialect(4).Execute();
            list = cli.FtAggregate(idxName, "@views:[200 200]").Execute();
            list = cli.FtAggregate(idxName, "@views!=200").Dialect(4).Execute();
            list = cli.FtAggregate(idxName, "-@views:[200 200]").Execute();
            list = cli.FtAggregate(idxName, "@views==200 | @views==300").Dialect(4).Execute();
            list = cli.FtAggregate(idxName, "*").Filter("views:[200 300]").Dialect(4).Execute();
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
    }
}
