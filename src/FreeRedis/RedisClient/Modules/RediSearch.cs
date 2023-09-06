using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string[] Ft_List() => Call("FT._LIST", rt => rt.ThrowOrValue<string[]>());

        //public string[] FtAggregate() => Call("FT._LIST", rt => rt.ThrowOrValue<string[]>());

        public void FtAliasAdd(string alias, string index) => Call("FT.ALIASADD".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");
        public void FtAliasDel(string alias) => Call("FT.ALIASDEL".Input(alias), rt => rt.ThrowOrValue<string>() == "OK");
        public void FtAliasUpdate(string alias, string index) => Call("FT.ALIASUPDATE".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");

        //public void FtAlter (string alias, string index) => Call("FT.ALTER".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");

        public Dictionary<string, object> FtConfigGet(string option, string value) => Call("FT.CONFIG".SubCommand("GET").Input(option, value), rt => rt.ThrowOrValue((a, _) => a.MapToHash<object>(rt.Encoding)));
        public void FtConfigSet(string option, string value) => Call("FT.CONFIG".SubCommand("SET").Input(option, value), rt => rt.ThrowOrValue<string>() == "OK");

        public void FtCreate(string index, FtCreateParams param) => Call(param.ToCommandPacket(index), rt => rt.ThrowOrValue<string>() == "OK");

        public void FtCursorDel(string index, long cursor_id) => Call("FT.CURSOR".SubCommand("DEL").Input(index, cursor_id), rt => rt.ThrowOrValue<string>() == "OK");
        public object FtCursorRead(string index, long cursor_id, int count = 0) => Call("FT.CURSOR".SubCommand("READ")
            .Input(index, cursor_id)
            .InputIf(count > 0, "COUNT", count), rt => rt.ThrowOrValue<object>());

        public long FtDictAdd(string dict, params string[] terms) => Call("FT.DICTADD".Input(dict).Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<long>());
        public long FtDictDel(string dict, params string[] terms) => Call("FT.DICTDEL".Input(dict).Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<long>());
        public string[] FtDictDump(string dict) => Call("FT.DICTDUMP".Input(dict), rt => rt.ThrowOrValue<string[]>());

        public void FtDropIndex(string index, bool dd = false) => Call("FT.DROPINDEX".Input(index).InputIf(dd, "DD"), rt => rt.ThrowOrValue<string>() == "OK");

        public string FtExplain(string index, string query, string dialect = null) => Call("FT.EXPLAIN".Input(index, query).InputIf(!string.IsNullOrEmpty(dialect), "DIALECT", dialect), rt => rt.ThrowOrValue<string>());
        public object FtInfo(string index) => Call("FT.INFO".Input(index), rt => rt.ThrowOrValue<object>());

        //public object FtProfile(string index) => Call("FT.PROFILE".Input(index), rt => rt.ThrowOrValue<object>());
        //public object FtSearch(string index) => Call("FT.SEARCH".Input(index), rt => rt.ThrowOrValue<object>());
        //public object FtSpellCheck(string index) => Call("FT.SPELLCHECK".Input(index), rt => rt.ThrowOrValue<object>());

        public Dictionary<string, string[]> FtSynDump(string index) => Call("FT.SYNDUMP".Input(index), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string[]>(rt.Encoding)));
        public void FtSynUpdate(string index, string synonym_group_id, bool skipInitialScan, params string[] terms) => Call("FT.SYNUPDATE"
            .Input(index, synonym_group_id)
            .InputIf(skipInitialScan, "SKIPINITIALSCAN")
            .Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<string>() == "OK");
        public string[] FtTagVals(string index, string field_name) => Call("FT.TAGVALS".Input(index, field_name), rt => rt.ThrowOrValue<string[]>());


    }

    public class FtAggregateBuilder
    {

    }
    public class FtCreateParams
    {
        public enum OnType { Hash, Json }
        public enum FieldType { Text, Tag, Numeric, Geo, Vector, GeoShape }
        public class Field
        {
            public string Name { get; set; }
            public string Alias { get; set; }
            public FieldType Type { get; set; }
            public bool Sortable { get; set; }
            public bool Unf { get; set; }
            public bool NoIndex { get; set; }
        }

        public OnType On { get; set; } = OnType.Hash;
        public string[] Prefix { get; set; }
        public string Filter { get; set; }
        public string Language { get; set; }
        public string LanguageField { get; set; }
        public decimal Score { get; set; }
        public decimal ScoreField { get; set; }
        public string PayLoadField { get; set; }
        public bool MaxTextFields { get; set; }
        public long Temporary { get; set; }
        public bool NoOffsets { get; set; }
        public bool NoHL { get; set; }
        public bool NoFields { get; set; }
        public bool NoFreqs { get; set; }
        public string[] Stopwords { get; set; }
        public bool SkipInitialScan { get; set; }
        public Field[] Schema { get; set; }

        public CommandPacket ToCommandPacket(string index)
        {
            var cmd = "FT.CREATE".Input(index).Input("ON", On.ToString().ToUpper());
            if (Prefix?.Any() == true) cmd.Input("PREFIX", Prefix.Length).Input(Prefix.Select(a => (object)a).ToArray());
            cmd
                .InputIf(!string.IsNullOrWhiteSpace(Filter), "FILTER", Filter)
                .InputIf(!string.IsNullOrWhiteSpace(Language), "LANGUAGE", Language)
                .InputIf(!string.IsNullOrWhiteSpace(LanguageField), "LANGUAGE_FIELD", LanguageField)
                .InputIf(Score > 0, "SCORE", Score)
                .InputIf(ScoreField > 0, "SCORE_FIELD", ScoreField)
                .InputIf(!string.IsNullOrWhiteSpace(PayLoadField), "PAYLOAD_FIELD", PayLoadField)
                .InputIf(MaxTextFields, "MAXTEXTFIELDS")
                .InputIf(Temporary > 0, "TEMPORARY", Temporary)
                .InputIf(NoOffsets, "NOOFFSETS")
                .InputIf(NoHL, "NOHL")
                .InputIf(NoFields, "NOFIELDS")
                .InputIf(NoFreqs, "NOFREQS");
            if (Stopwords?.Any() == true) cmd.Input("STOPWORDS", Prefix.Length).Input(Stopwords.Select(a => (object)a).ToArray());
            cmd
                .InputIf(SkipInitialScan, "SKIPINITIALSCAN");
            if (Schema != null)
            {
                cmd.Input("SCHEMA");
                foreach(var field in Schema)
                {
                    cmd.Input(field.Name)
                        .InputIf(!string.IsNullOrWhiteSpace(field.Alias), "AS", field.Alias)
                        .Input(field.Type.ToString().ToUpper())
                        .InputIf(field.Sortable, "SORTABLE")
                        .InputIf(field.Sortable && field.Unf, "UNF")
                        .InputIf(field.NoIndex, "NOINDEX");
                }
            }
            return cmd;
        }
    }
}
