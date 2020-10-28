using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class SortedSetsTests : TestBase
    {
        [Fact]
        public void BZPopMin()
        {
            var key1 = "BZPopMin1" + Guid.NewGuid();
            cli.Del(key1);
            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");

            var r1 = cli.BZPopMin(key1, 1);
            Assert.Equal("member1", r1.member);
            Assert.Equal(100.991m, r1.score);

            var r2 = cli.BZPopMin(key1, 1);
            Assert.Equal("member2", r2.member);
            Assert.Equal(100.992m, r2.score);

            var r3 = cli.BZPopMin(key1, 1);
            Assert.Null(r3);

            var key2 = "BZPopMin2" + Guid.NewGuid();
            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");
            cli.ZAdd(key2, 100.991m, "member11");
            cli.ZAdd(key2, 100.992m, "member22");

            var r4 = cli.BZPopMin(new[] { key1, key2 }, 1);
            Assert.Equal(key1, r4.key);
            Assert.Equal("member1", r4.value.member);
            Assert.Equal(100.991m, r4.value.score);

            var r5 = cli.BZPopMin(new[] { key1, key2 }, 1);
            Assert.Equal(key1, r5.key);
            Assert.Equal("member2", r5.value.member);
            Assert.Equal(100.992m, r5.value.score);

            var r6 = cli.BZPopMin(new[] { key1, key2 }, 1);
            Assert.Equal(key2, r6.key);
            Assert.Equal("member11", r6.value.member);
            Assert.Equal(100.991m, r6.value.score);

            var r7 = cli.BZPopMin(new[] { key1, key2 }, 1);
            Assert.Equal(key2, r7.key);
            Assert.Equal("member22", r7.value.member);
            Assert.Equal(100.992m, r7.value.score);

            var r8 = cli.ZPopMin(key1);
            Assert.Null(r8);
        }

        [Fact]
        public void BZPopMax()
        {
            var key1 = "BZPopMax1" + Guid.NewGuid();
            cli.Del(key1);
            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");

            var r2 = cli.BZPopMax(key1, 1);
            Assert.Equal("member2", r2.member);
            Assert.Equal(100.992m, r2.score);

            var r1 = cli.BZPopMax(key1, 1);
            Assert.Equal("member1", r1.member);
            Assert.Equal(100.991m, r1.score);

            var r3 = cli.BZPopMax(key1, 1);
            Assert.Null(r3);

            var key2 = "BZPopMax2" + Guid.NewGuid();
            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");
            cli.ZAdd(key2, 100.991m, "member11");
            cli.ZAdd(key2, 100.992m, "member22");

            var r4 = cli.BZPopMax(new[] { key1, key2 }, 1);
            Assert.Equal(key1, r4.key);
            Assert.Equal("member2", r4.value.member);
            Assert.Equal(100.992m, r4.value.score);

            var r5 = cli.BZPopMax(new[] { key1, key2 }, 1);
            Assert.Equal(key1, r5.key);
            Assert.Equal("member1", r5.value.member);
            Assert.Equal(100.991m, r5.value.score);

            var r6 = cli.BZPopMax(new[] { key1, key2 }, 1);
            Assert.Equal(key2, r6.key);
            Assert.Equal("member22", r6.value.member);
            Assert.Equal(100.992m, r6.value.score);

            var r7 = cli.BZPopMax(new[] { key1, key2 }, 1);
            Assert.Equal(key2, r7.key);
            Assert.Equal("member11", r7.value.member);
            Assert.Equal(100.991m, r7.value.score);

            var r8 = cli.ZPopMin(key1);
            Assert.Null(r8);
        }

        [Fact]
        public void ZAdd()
        {
            var key1 = "ZAdd1" + Guid.NewGuid();
            cli.Del(key1);
            Assert.Equal(1, cli.ZAdd(key1, 100.991m, "member1"));
            Assert.Equal(1, cli.ZAdd(key1, 100.992m, "member2"));
            Assert.Equal(2, cli.ZAdd(key1, new[] { 
                new ZMember("member3", 100.993m), 
                new ZMember("member4", 100.994m) 
            }));
            Assert.Equal(2, cli.ZAdd(key1, 100.995m, "member5", 100.996m, "member6"));
            Assert.Equal(3, cli.ZAdd(key1, 100.997m, "member7", 100.998m, "member8", 100.999m, "member9"));
            Assert.Equal(2, cli.ZAdd(key1, new[] {
                new ZMember("member91", 100.9991m),
                new ZMember("member92", 100.9992m)
            }, null, false));
            Assert.Equal(2, cli.ZAdd(key1, new[] {
                new ZMember("member93", 100.999m),
                new ZMember("member94", 100.998m)
            }, null, true));

            Assert.Equal(13, cli.ZCard(key1));


            //redis v6.2
            //var r16 = cli.ZAdd(key1, new[] {
            //    new ZMember("member91", 100.9991m),
            //    new ZMember("member92", 100.9992m)
            //}, ZAddThan.gt, false);
            //var r17 = cli.ZAdd(key1, new[] {
            //    new ZMember("member93", 100.999m),
            //    new ZMember("member94", 100.998m)
            //}, ZAddThan.lt, false);
            //var r18 = cli.ZAdd(key1, new[] {
            //    new ZMember("member95", 100.9991m),
            //    new ZMember("member96", 100.9992m)
            //}, ZAddThan.gt, true);
            //var r19 = cli.ZAdd(key1, new[] {
            //    new ZMember("member97", 100.999m),
            //    new ZMember("member98", 100.998m)
            //}, ZAddThan.lt, true);
        }

        [Fact]
        public void ZAddNx()
        {
            var key1 = "ZAddNx1" + Guid.NewGuid();
            cli.Del(key1);
            Assert.Equal(1, cli.ZAddNx(key1, 100.991m, "member1"));
            Assert.Equal(1, cli.ZAddNx(key1, 100.992m, "member2"));
            Assert.Equal(2, cli.ZAddNx(key1, new[] {
                new ZMember("member3", 100.993m),
                new ZMember("member4", 100.994m)
            }));
            Assert.Equal(2, cli.ZAddNx(key1, 100.995m, "member5", 100.996m, "member6"));
            Assert.Equal(3, cli.ZAddNx(key1, 100.997m, "member7", 100.998m, "member8", 100.999m, "member9"));
            Assert.Equal(2, cli.ZAddNx(key1, new[] {
                new ZMember("member91", 100.9991m),
                new ZMember("member92", 100.9992m)
            }, null, false));
            Assert.Equal(2, cli.ZAddNx(key1, new[] {
                new ZMember("member93", 100.999m),
                new ZMember("member94", 100.998m)
            }, null, true));

            Assert.Equal(13, cli.ZCard(key1));

            Assert.Equal(0, cli.ZAddNx(key1, 100.991m, "member1"));
            Assert.Equal(0, cli.ZAddNx(key1, 100.992m, "member2"));
            Assert.Equal(0, cli.ZAddNx(key1, new[] {
                new ZMember("member3", 100.993m),
                new ZMember("member4", 100.994m)
            }));
            Assert.Equal(0, cli.ZAddNx(key1, 100.995m, "member5", 100.996m, "member6"));
            Assert.Equal(0, cli.ZAddNx(key1, 100.997m, "member7", 100.998m, "member8", 100.999m, "member9"));
            Assert.Equal(0, cli.ZAddNx(key1, new[] {
                new ZMember("member91", 100.9991m),
                new ZMember("member92", 100.9992m)
            }, null, false));
            Assert.Equal(0, cli.ZAddNx(key1, new[] {
                new ZMember("member93", 100.999m),
                new ZMember("member94", 100.998m)
            }, null, true));


            //redis v6.2
            //var r16 = cli.ZAddNx(key1, new[] {
            //    new ZMember("member91", 100.9991m),
            //    new ZMember("member92", 100.9992m)
            //}, ZAddNxThan.gt, false);
            //var r17 = cli.ZAddNx(key1, new[] {
            //    new ZMember("member93", 100.999m),
            //    new ZMember("member94", 100.998m)
            //}, ZAddNxThan.lt, false);
            //var r18 = cli.ZAddNx(key1, new[] {
            //    new ZMember("member95", 100.9991m),
            //    new ZMember("member96", 100.9992m)
            //}, ZAddNxThan.gt, true);
            //var r19 = cli.ZAddNx(key1, new[] {
            //    new ZMember("member97", 100.999m),
            //    new ZMember("member98", 100.998m)
            //}, ZAddNxThan.lt, true);
        }

        [Fact]
        public void ZAddXx()
        {
            var key1 = "ZAddXx1" + Guid.NewGuid();
            cli.Del(key1);
            Assert.Equal(1, cli.ZAdd(key1, 100.991m, "member1"));
            Assert.Equal(1, cli.ZAdd(key1, 100.992m, "member2"));
            Assert.Equal(2, cli.ZAdd(key1, new[] {
                new ZMember("member3", 100.993m),
                new ZMember("member4", 100.994m)
            }));
            Assert.Equal(2, cli.ZAdd(key1, 100.995m, "member5", 100.996m, "member6"));
            Assert.Equal(3, cli.ZAdd(key1, 100.997m, "member7", 100.998m, "member8", 100.999m, "member9"));
            Assert.Equal(2, cli.ZAdd(key1, new[] {
                new ZMember("member91", 100.9991m),
                new ZMember("member92", 100.9992m)
            }, null, false));
            Assert.Equal(2, cli.ZAdd(key1, new[] {
                new ZMember("member93", 100.999m),
                new ZMember("member94", 100.998m)
            }, null, false));

            Assert.Equal(13, cli.ZCard(key1));


            Assert.Equal(0, cli.ZAddXx(key1, 100.1991m, "member1"));
            Assert.Equal(0, cli.ZAddXx(key1, 100.1992m, "member2"));
            Assert.Equal(0, cli.ZAddXx(key1, new[] {
                new ZMember("member3", 100.1993m),
                new ZMember("member4", 100.1994m)
            }));
            Assert.Equal(0, cli.ZAddXx(key1, 100.995m, "member5", 100.996m, "member6"));
            Assert.Equal(0, cli.ZAddXx(key1, 100.997m, "member7", 100.998m, "member8", 100.999m, "member9"));
            Assert.Equal(0, cli.ZAddXx(key1, new[] {
                new ZMember("member91", 100.19991m),
                new ZMember("member92", 100.19992m)
            }, null, false));
            Assert.Equal(2, cli.ZAddXx(key1, new[] {
                new ZMember("member93", 100.1999m),
                new ZMember("member94", 100.1998m)
            }, null, true));


            //redis v6.2
            //var r16 = cli.ZAddXx(key1, new[] {
            //    new ZMember("member91", 100.9991m),
            //    new ZMember("member92", 100.9992m)
            //}, ZAddXxThan.gt, false);
            //var r17 = cli.ZAddXx(key1, new[] {
            //    new ZMember("member93", 100.999m),
            //    new ZMember("member94", 100.998m)
            //}, ZAddXxThan.lt, false);
            //var r18 = cli.ZAddXx(key1, new[] {
            //    new ZMember("member95", 100.9991m),
            //    new ZMember("member96", 100.9992m)
            //}, ZAddXxThan.gt, true);
            //var r19 = cli.ZAddXx(key1, new[] {
            //    new ZMember("member97", 100.999m),
            //    new ZMember("member98", 100.998m)
            //}, ZAddXxThan.lt, true);
        }

        [Fact]
        public void ZCard()
        {
            var key1 = "ZCard1";
            cli.Del(key1);
            Assert.Equal(0, cli.ZCard(key1));
            Assert.Equal(1, cli.ZAdd(key1, 100.991m, "member1"));
            Assert.Equal(0, cli.ZAdd(key1, 100.991m, "member1"));
            Assert.Equal(1, cli.ZCard(key1));
        }

        [Fact]
        public void ZCount()
        {
            var key1 = "ZCount1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(5, cli.ZCount(key1, 0, 12));
            Assert.Equal(5, cli.ZCount(key1, 0, 13));
            Assert.Equal(5, cli.ZCount(key1, 1, 12));
            Assert.Equal(5, cli.ZCount(key1, 1, 13));
            Assert.Equal(2, cli.ZCount(key1, 10, 11));
            Assert.Equal(5, cli.ZCount(key1, "-inf", "+inf"));
        }

        [Fact]
        public void ZIncrBy()
        {
            var key1 = "ZIncrBy1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1");
            Assert.Equal(1, cli.ZScore(key1, "m1"));
            Assert.Equal(3, cli.ZIncrBy(key1, 2, "m1"));
            Assert.Equal(3, cli.ZScore(key1, "m1"));
        }

        [Fact]
        public void ZLexCount()
        {
            var key1 = "ZZLexCount1";
            cli.Del(key1);
            cli.ZAdd(key1, 0, "a", 0, "b", 0, "c", 0, "d", 0, "e");
            cli.ZAdd(key1, 0, "f", 0, "g", 0, "c", 0, "d", 0, "e");
            Assert.Equal(7, cli.ZLexCount(key1, "-", "+"));
            Assert.Equal(5, cli.ZLexCount(key1, "[b", "[f"));
        }

        [Fact]
        public void ZPopMin()
        {
            var key1 = "ZPopMin1" + Guid.NewGuid();
            cli.Del(key1);
            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");

            var r1 = cli.ZPopMin(key1);
            Assert.Equal("member1", r1.member);
            Assert.Equal(100.991m, r1.score);

            var r2 = cli.ZPopMin(key1);
            Assert.Equal("member2", r2.member);
            Assert.Equal(100.992m, r2.score);

            var r3 = cli.ZPopMin(key1);
            Assert.Null(r3);

            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");
            cli.ZAdd(key1, 100.995m, "member3");

            var r4 = cli.ZPopMin(key1, 1);
            Assert.Single(r4);
            Assert.Equal("member1", r4[0].member);
            Assert.Equal(100.991m, r4[0].score);

            var r5 = cli.ZPopMin(key1, 2);
            Assert.Equal(2, r5.Length);
            Assert.Equal("member2", r5[0].member);
            Assert.Equal(100.992m, r5[0].score);
            Assert.Equal("member3", r5[1].member);
            Assert.Equal(100.995m, r5[1].score);

            var r6 = cli.ZPopMin(key1);
            Assert.Null(r6);

            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");
            cli.ZAdd(key1, 100.995m, "member3");

            var r7 = cli.ZPopMin(key1, 4);
            Assert.Equal(3, r7.Length);
            Assert.Equal("member1", r7[0].member);
            Assert.Equal(100.991m, r7[0].score);
            Assert.Equal("member2", r7[1].member);
            Assert.Equal(100.992m, r7[1].score);
            Assert.Equal("member3", r7[2].member);
            Assert.Equal(100.995m, r7[2].score);

            var r8 = cli.ZPopMin(key1);
            Assert.Null(r8);
        }

        [Fact]
        public void ZPopMax()
        {
            var key1 = "ZPopMax1" + Guid.NewGuid();
            cli.Del(key1);
            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");

            var r1 = cli.ZPopMax(key1);
            Assert.Equal("member2", r1.member);
            Assert.Equal(100.992m, r1.score);

            var r2 = cli.ZPopMax(key1);
            Assert.Equal("member1", r2.member);
            Assert.Equal(100.991m, r2.score);

            var r3 = cli.ZPopMax(key1);
            Assert.Null(r3);

            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");
            cli.ZAdd(key1, 100.995m, "member3");

            var r4 = cli.ZPopMax(key1, 1);
            Assert.Single(r4);
            Assert.Equal("member3", r4[0].member);
            Assert.Equal(100.995m, r4[0].score);

            var r5 = cli.ZPopMax(key1, 2);
            Assert.Equal(2, r5.Length);
            Assert.Equal("member2", r5[0].member);
            Assert.Equal(100.992m, r5[0].score);
            Assert.Equal("member1", r5[1].member);
            Assert.Equal(100.991m, r5[1].score);

            var r6 = cli.ZPopMax(key1);
            Assert.Null(r6);

            cli.ZAdd(key1, 100.991m, "member1");
            cli.ZAdd(key1, 100.992m, "member2");
            cli.ZAdd(key1, 100.995m, "member3");

            var r7 = cli.ZPopMax(key1, 4);
            Assert.Equal(3, r7.Length);
            Assert.Equal("member3", r7[0].member);
            Assert.Equal(100.995m, r7[0].score);
            Assert.Equal("member2", r7[1].member);
            Assert.Equal(100.992m, r7[1].score);
            Assert.Equal("member1", r7[2].member);
            Assert.Equal(100.991m, r7[2].score);

            var r8 = cli.ZPopMax(key1);
            Assert.Null(r8);
        }

        [Fact]
        public void ZRange()
        {
            var key1 = "ZRange1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRange(key1, 0, 12);
            Assert.Equal(5, r1.Length);
            Assert.Equal("m1", r1[0]);
            Assert.Equal("m2", r1[1]);
            Assert.Equal("m10", r1[2]);
            Assert.Equal("m11", r1[3]);
            Assert.Equal("m12", r1[4]);
            var r2 = cli.ZRange(key1, 2, 3);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m10", r2[0]);
            Assert.Equal("m11", r2[1]);
        }

        [Fact]
        public void ZRangeWithScores()
        {
            var key1 = "ZRangeWithScores1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRangeWithScores(key1, 0, 12);
            Assert.Equal(5, r1.Length);
            Assert.Equal("m1", r1[0].member);
            Assert.Equal("m2", r1[1].member);
            Assert.Equal("m10", r1[2].member);
            Assert.Equal("m11", r1[3].member);
            Assert.Equal("m12", r1[4].member);
            Assert.Equal(1, r1[0].score);
            Assert.Equal(2, r1[1].score);
            Assert.Equal(10, r1[2].score);
            Assert.Equal(11, r1[3].score);
            Assert.Equal(12, r1[4].score);
            var r2 = cli.ZRangeWithScores(key1, 2, 3);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m10", r2[0].member);
            Assert.Equal("m11", r2[1].member);
            Assert.Equal(10, r2[0].score);
            Assert.Equal(11, r2[1].score);
        }

        [Fact]
        public void ZRangeByLex()
        {
            var key1 = "ZRangeByLex1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRangeByLex(key1, "-", "+");
            Assert.Equal(5, r1.Length);
            Assert.Equal("m1", r1[0]);
            Assert.Equal("m2", r1[1]);
            Assert.Equal("m10", r1[2]);
            Assert.Equal("m11", r1[3]);
            Assert.Equal("m12", r1[4]);
            var r2 = cli.ZRangeByLex(key1, "-", "+", 2, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m10", r2[0]);
            Assert.Equal("m11", r2[1]);
        }

        [Fact]
        public void ZRangeByScore()
        {
            var key1 = "ZRangeByScore1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRangeByScore(key1, "-inf", "+inf");
            Assert.Equal(5, r1.Length);
            Assert.Equal("m1", r1[0]);
            Assert.Equal("m2", r1[1]);
            Assert.Equal("m10", r1[2]);
            Assert.Equal("m11", r1[3]);
            Assert.Equal("m12", r1[4]);
            var r2 = cli.ZRangeByScore(key1, 1, 11, 2, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m10", r2[0]);
            Assert.Equal("m11", r2[1]);
        }

        [Fact]
        public void ZRangeByScoreWithScores()
        {
            var key1 = "ZRangeByScoreWithScores1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRangeByScoreWithScores(key1, "-inf", "+inf");
            Assert.Equal(5, r1.Length);
            Assert.Equal("m1", r1[0].member);
            Assert.Equal("m2", r1[1].member);
            Assert.Equal("m10", r1[2].member);
            Assert.Equal("m11", r1[3].member);
            Assert.Equal("m12", r1[4].member);
            Assert.Equal(1, r1[0].score);
            Assert.Equal(2, r1[1].score);
            Assert.Equal(10, r1[2].score);
            Assert.Equal(11, r1[3].score);
            Assert.Equal(12, r1[4].score);
            var r2 = cli.ZRangeByScoreWithScores(key1, 1, 11, 2, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m10", r2[0].member);
            Assert.Equal("m11", r2[1].member);
            Assert.Equal(10, r2[0].score);
            Assert.Equal(11, r2[1].score);
        }

        [Fact]
        public void ZRank()
        {
            var key1 = "ZRank1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(2, cli.ZRank(key1, "m10"));
        }

        [Fact]
        public void ZRem()
        {
            var key1 = "ZRem1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(1, cli.ZRem(key1, "m10"));
            Assert.Equal(0, cli.ZRem(key1, "m10"));
            Assert.Equal(2, cli.ZRem(key1, "m11", "m12"));
            Assert.Equal(2, cli.ZRem(key1, "m1", "m2", "m2"));
        }

        [Fact]
        public void ZRemRangeByLex()
        {
            var key1 = "ZRemRangeByLex1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(5, cli.ZRemRangeByLex(key1, "-", "+"));
        }

        [Fact]
        public void ZRemRangeByRank()
        {
            var key1 = "ZRemRangeByRank1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(5, cli.ZRemRangeByRank(key1, 0, -1));
        }

        [Fact]
        public void ZRemRangeByScore()
        {
            var key1 = "ZRemRangeByScore1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(5, cli.ZRemRangeByScore(key1, "-inf", "+inf"));
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(2, cli.ZRemRangeByScore(key1, 10, 11));
        }

        [Fact]
        public void ZRevRange()
        {
            var key1 = "ZRevRange1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRevRange(key1, 0, 12);
            Assert.Equal(5, r1.Length);
            Assert.Equal("m12", r1[0]);
            Assert.Equal("m11", r1[1]);
            Assert.Equal("m10", r1[2]);
            Assert.Equal("m2", r1[3]);
            Assert.Equal("m1", r1[4]);
            var r2 = cli.ZRevRange(key1, 1, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m11", r2[0]);
            Assert.Equal("m10", r2[1]);
        }

        [Fact]
        public void ZRevRangeWithScores()
        {
            var key1 = "ZRevRangeWithScores1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRevRangeWithScores(key1, 0, 12);
            Assert.Equal(5, r1.Length);
            Assert.Equal("m12", r1[0].member);
            Assert.Equal("m11", r1[1].member);
            Assert.Equal("m10", r1[2].member);
            Assert.Equal("m2", r1[3].member);
            Assert.Equal("m1", r1[4].member);
            Assert.Equal(12, r1[0].score);
            Assert.Equal(11, r1[1].score);
            Assert.Equal(10, r1[2].score);
            Assert.Equal(2, r1[3].score);
            Assert.Equal(1, r1[4].score);
            var r2 = cli.ZRevRangeWithScores(key1, 1, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m11", r2[0].member);
            Assert.Equal("m10", r2[1].member);
            Assert.Equal(11, r2[0].score);
            Assert.Equal(10, r2[1].score);
        }

        [Fact]
        public void ZRevRangeByLex()
        {
            var key1 = "ZRevRangeByLex1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRevRangeByLex(key1, "+", "-");
            Assert.Equal(5, r1.Length);
            Assert.Equal("m12", r1[0]);
            Assert.Equal("m11", r1[1]);
            Assert.Equal("m10", r1[2]);
            Assert.Equal("m2", r1[3]);
            Assert.Equal("m1", r1[4]);
            var r2 = cli.ZRevRangeByLex(key1, "+", "-", 2, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m10", r2[0]);
            Assert.Equal("m2", r2[1]);
        }

        [Fact]
        public void ZRevRangeByScore()
        {
            var key1 = "ZRevRangeByScore1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRevRangeByScore(key1, "+inf", "-inf");
            Assert.Equal(5, r1.Length);
            Assert.Equal("m12", r1[0]);
            Assert.Equal("m11", r1[1]);
            Assert.Equal("m10", r1[2]);
            Assert.Equal("m2", r1[3]);
            Assert.Equal("m1", r1[4]);
            var r2 = cli.ZRevRangeByScore(key1, 11, 1, 2, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m2", r2[0]);
            Assert.Equal("m1", r2[1]);
        }

        [Fact]
        public void ZRevRangeByScoreWithScores()
        {
            var key1 = "ZRevRangeByScoreWithScores1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            var r1 = cli.ZRevRangeByScoreWithScores(key1, "+inf", "-inf");
            Assert.Equal(5, r1.Length);
            Assert.Equal("m12", r1[0].member);
            Assert.Equal("m11", r1[1].member);
            Assert.Equal("m10", r1[2].member);
            Assert.Equal("m2", r1[3].member);
            Assert.Equal("m1", r1[4].member);
            Assert.Equal(12, r1[0].score);
            Assert.Equal(11, r1[1].score);
            Assert.Equal(10, r1[2].score);
            Assert.Equal(2, r1[3].score);
            Assert.Equal(1, r1[4].score);
            var r2 = cli.ZRevRangeByScoreWithScores(key1, 11, 1, 2, 2);
            Assert.Equal(2, r2.Length);
            Assert.Equal("m2", r2[0].member);
            Assert.Equal("m1", r2[1].member);
            Assert.Equal(2, r2[0].score);
            Assert.Equal(1, r2[1].score);
        }

        [Fact]
        public void ZRevRank()
        {
            var key1 = "ZRevRank1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(2, cli.ZRevRank(key1, "m10"));
        }

        [Fact]
        public void ZScore()
        {
            var key1 = "ZRevRank1";
            cli.Del(key1);
            cli.ZAdd(key1, 1, "m1", 2, "m2", 10, "m10", 11, "m11", 12, "m12");
            Assert.Equal(1, cli.ZScore(key1, "m1"));
            Assert.Equal(2, cli.ZScore(key1, "m2"));
            Assert.Equal(10, cli.ZScore(key1, "m10"));
            Assert.Equal(11, cli.ZScore(key1, "m11"));
            Assert.Equal(12, cli.ZScore(key1, "m12"));
        }
    }
}
