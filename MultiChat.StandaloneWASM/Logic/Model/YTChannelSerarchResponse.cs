using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiChat.StandaloneWASM.Logic.Model.Channel
{

  public class YTChannelSerarchResponse
  {
    public string kind { get; set; }
    public string etag { get; set; }
    public Pageinfo pageInfo { get; set; }
    public Item[] items { get; set; }
  }

  public class Pageinfo
  {
    public int totalResults { get; set; }
    public int resultsPerPage { get; set; }
  }

  public class Item
  {
    public string kind { get; set; }
    public string etag { get; set; }
    public string id { get; set; }
    public Snippet snippet { get; set; }
  }

  public class Snippet
  {
    public string title { get; set; }
    public string description { get; set; }
    public string customUrl { get; set; }
    public DateTime publishedAt { get; set; }
    public Thumbnails thumbnails { get; set; }
    public Localized localized { get; set; }
    public string country { get; set; }
  }

  public class Thumbnails
  {
    public Default _default { get; set; }
    public Medium medium { get; set; }
    public High high { get; set; }
  }

  public class Default
  {
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
  }

  public class Medium
  {
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
  }

  public class High
  {
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
  }

  public class Localized
  {
    public string title { get; set; }
    public string description { get; set; }
  }
}

