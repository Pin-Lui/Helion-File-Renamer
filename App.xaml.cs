using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Windows;

namespace Helion
{
  public partial class App : Application
  {
    public List<ShowDetails> ShowBuffer { get; set; } = [];
  }

  public sealed class ShowDetails
  {
    public string Title { get; set; }
    public string Directory { get; set; }
    public string Tvrage { get; set; }
    public string TVmaze { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public string NumberOfEpisodes { get; set; }
    public string RunTime { get; set; }
    public string Network { get; set; }
    public string Country { get; set; }
    public string Onhiatus { get; set; }
    public string Onhiatusdesc { get; set; }
  }

  public sealed class ShowDetailsMapping : ClassMap<ShowDetails>
  {
    public ShowDetailsMapping()
    {
      Map(m => m.Title).Name("title");
      Map(m => m.Directory).Name("directory");
      Map(m => m.Tvrage).Name("tvrage");
      Map(m => m.TVmaze).Name("TVmaze");
      Map(m => m.StartDate).Name("start date");
      Map(m => m.EndDate).Name("end date");
      Map(m => m.NumberOfEpisodes).Name("number of episodes");
      Map(m => m.RunTime).Name("run time");
      Map(m => m.Network).Name("network");
      Map(m => m.Country).Name("country");
      Map(m => m.Onhiatus).Name("onhiatus");
      Map(m => m.Onhiatusdesc).Name("onhiatusdesc");
    }
  }

  internal sealed class EpisodeDetails
  {
    public string EPNumber { get; set; }
    public string Season { get; set; }
    public string Episode { get; set; }
    public string Airdate { get; set; }
    public string Title { get; set; }
    public string TvmazeLink { get; set; }
  }

  internal sealed class EpisodeDetailsMapping : ClassMap<EpisodeDetails>
  {
    public EpisodeDetailsMapping()
    {
      Map(m => m.EPNumber).Name("number");
      Map(m => m.Season).Name("season");
      Map(m => m.Episode).Name("episode");
      Map(m => m.Airdate).Name("airdate");
      Map(m => m.Title).Name("title");
      Map(m => m.TvmazeLink).Name("tvmaze link");
    }
  }
}
