using System.Globalization;
using CsvHelper;
using System.Linq;

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

List<UserRating> GetUserRatings(string fileName)
{
    using (var reader = new StreamReader(fileName))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        csv.Configuration.MissingFieldFound = null;
        var ratings = csv.GetRecords<UserRating>();
        return new List<UserRating>(ratings);
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

Dictionary<string, double[]> GetUserPreferences(List<UserRating> userRatings, List<Movie> listMovies)
{
    var gen = new List<string>{"Drama", "Comedy", "Documentary", "Fiction", "Action",
        "Romance", "Animation", "Thriller"};

    var userPreferences = new Dictionary<string, double[]>();
    
    foreach(var userRating in userRatings)
    {
        if (!userPreferences.ContainsKey(userRating.user_id))
            userPreferences[userRating.user_id] = new double[8];
        
        var movieGenre = listMovies.Find(m => m.movie_id == userRating.movie_id)?.genres;
        
        if (movieGenre != null && gen.Contains(movieGenre))
            userPreferences[userRating.user_id][gen.IndexOf(movieGenre)] += userRating.rating_val;
    }
    
    foreach(var userId in userPreferences.Keys.ToList())
    {
        for(int i = 0; i < 8; i++)
            userPreferences[userId][i] /= 10.0; //divide by 10 cuz we need point from 0 to 1
    }
    
    return userPreferences;
}

var m = GetMovie("movie_data.csv");
var listMovies = ArrangeMovies(m);
var userRatings = GetUserRatings("rating.csv");
var userPreferences = GetUserPreferences(userRatings, listMovies);

foreach(var userId in userPreferences.Keys)
{
    Console.WriteLine(userId + ": [" + string.Join(", ", userPreferences[userId]) + "]");
}

public class Movie
{
    public string genres { get; set; }
    public string movie_title { get; set; }
    public string movie_id { get; set; }
}

public class UserRating
{
    public string _id { get; set; }
    public string movie_id { get; set; }
    public int rating_val { get; set; }
    public string user_id { get; set; }
}
