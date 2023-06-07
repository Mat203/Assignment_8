using System.Globalization;
using CsvHelper;


List<Movie> GetMovie(string fileName)
{
    using (var reader = new StreamReader(fileName))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        csv.Configuration.MissingFieldFound = null;
        var movies = csv.GetRecords<Movie>();
        return new List<Movie>(movies);
    }
}

List<Movie> ArrangeMovies(List<Movie> listM)
{
    foreach (var el in listM)
    {
        if (el.genres == "[]")
        {
            el.genres = "NaN";
        }
        if (el.genres.Contains("Fantasy")) // as we should combine fantasy and sci-fi into one
        {
            el.genres = el.genres.Replace("Fantasy", "Fiction");
        }
        else if (el.genres.Contains("Science Fiction"))
        {
            el.genres = el.genres.Replace("Science Fiction", "Fiction");
        }
        else if (el.genres.Contains("Adventure")) // as we should add adventure to action
        {
            el.genres = el.genres.Replace("Adventure", "Action");
        }
    
        el.genres = el.genres.Trim('[', ']', '"');
        el.genres = el.genres.Replace('"'.ToString(), "");
        
    }

    var gen = new List<string>{"Drama", "Comedy", "Documentary", "Fiction", "Action", 
        "Romance", "Animation", "Thriller"};

    var shorter = listM.FindAll(movie => gen.Contains(movie.genres));
    return shorter;
}

var m = GetMovie("movie_data.csv");
var listMovies = ArrangeMovies(m);


Console.WriteLine('k');

public class Movie
{
    public string genres { get; set; }
    public string movie_title { get; set; }
    public string movie_id { get; set; }
}