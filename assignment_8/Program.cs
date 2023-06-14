using System.Globalization;
using CsvHelper;
using System.Linq;

List<Movie> GetMovies(string fileName)
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
    var userSum = new Dictionary<string, double>();

    foreach (var userRating in userRatings)
    {
        if (!userPreferences.ContainsKey(userRating.user_id))
        {
            userPreferences[userRating.user_id] = new double[8];
            userSum[userRating.user_id] = 0;
        }

        var movieGenre = listMovies.Find(m => m.movie_id == userRating.movie_id)?.genres;

        if (movieGenre != null && gen.Contains(movieGenre))
        {
            userPreferences[userRating.user_id][gen.IndexOf(movieGenre)] += userRating.rating_val;
            userSum[userRating.user_id] += userRating.rating_val;
        }
    }

    foreach (var userId in userPreferences.Keys.ToList())
    {
        for (int i = 0; i < 8; i++)
        {
            userPreferences[userId][i] /= userSum[userId];
            userPreferences[userId][i] = Math.Round(userPreferences[userId][i], 5);
        }
    }

    return userPreferences;
}

void CompareUsers(Dictionary<string, double[]> new_UserPreferences, Dictionary<string, double[]> UserPreferences)
{
    var newUser = new_UserPreferences.Single();

    int matchCount = 0;

    foreach (var existingUser in UserPreferences)
    {
        bool isMatch = true;

        for (int i = 0; i < newUser.Value.Length; i++)
        {
            if (Math.Abs(newUser.Value[i] - existingUser.Value[i]) >= 0.25)
            {
                isMatch = false;
                break;
            }
        }

        if (isMatch)
        {
            Console.WriteLine("Matching user:");
            Console.WriteLine("User ID: " + existingUser.Key);
            Console.WriteLine("Preferences: [" + string.Join(", ", existingUser.Value) + "]");
            Console.WriteLine();
            matchCount++;

            if (matchCount == 3)
                break;
        }
    }
}

string GetFavoriteGenre(Dictionary<string, int> genreCounts)
{
    int maxCount = genreCounts.Values.Max();

    var favoriteGenres = genreCounts.Where(kv => kv.Value == maxCount).Select(kv => kv.Key).ToList();

    favoriteGenres.Sort();
    return favoriteGenres.First();
}

Dictionary<string, List<Movie>> BestFromEachGenre(List<Movie> listM)
{
    var genres = new List<string>{"Drama", "Comedy", "Documentary", "Fiction", "Action",
        "Romance", "Animation", "Thriller"};
    var dictOfBest = new Dictionary<string, List<Movie>>();

    foreach (var g in genres)
    {
        var listGenre = new List<Movie>();
        foreach (var m in listM)
        {
            if (m.genres.Contains(g))
            {
                listGenre.Add(m);
            }
        }
        var bestInGenre = listGenre.OrderBy(movie => Convert.ToDouble(movie.vote_average)).Take(2).ToList();
        dictOfBest[g] = bestInGenre;
    }
    return dictOfBest;
}


var listMovies = ArrangeMovies(GetMovies("movie_data.csv"));
var userRatings = GetUserRatings("rating.csv");
var userPreferences = GetUserPreferences(userRatings, listMovies);
var dictOfBests = BestFromEachGenre(listMovies);


foreach (var userId in userPreferences.Keys)
{
    Console.WriteLine(userId + ": [" + string.Join(", ", userPreferences[userId]) + "]");
}


Recommend();


void Recommend()
{
    var genres = new List<string>{"Drama", "Comedy", "Documentary", "Fiction", "Action",
        "Romance", "Animation", "Thriller"};
    List<UserRating> new_user = new List<UserRating>();
    int count = 0;
    var genreCounts = new Dictionary<string, int>(); // Хранит количество просмотренных фильмов каждого жанра
    foreach (var g in genres) genreCounts[g] = 0;
    
    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (input == "recommend" && count > 2)
        {
            var new_userPreferences = GetUserPreferences(new_user, listMovies);
            foreach (var userId in new_userPreferences.Keys)
            {
                Console.WriteLine(userId + ": [" + string.Join(", ", new_userPreferences[userId]) + "]");
            }
            CompareUsers(new_userPreferences, userPreferences);
            var favoriteGenre = GetFavoriteGenre(genreCounts);
            Console.WriteLine("Your favorite genre is: " + favoriteGenre);

            break;
        }
        
        // discovery 
        else if (input == "discovery")
        {
            foreach (var g in dictOfBests.Keys)
            {
                foreach (var movie in dictOfBests[g])
                {
                    Console.WriteLine($"have you seen '{movie.movie_title}'? (yes/no)");
                    var answer = Console.ReadLine();
                    if (answer == "yes")
                    {
                        Console.WriteLine("how would you rate it?");
                        var rate = Convert.ToDouble(Console.ReadLine());
                        var newRating = new UserRating()
                        {
                            movie_id = movie.movie_id,
                            _id = "0",
                            rating_val = (int)rate,
                            user_id = "user123"
                        };
                        new_user.Add(newRating);
                        genreCounts[g]++;
                        count++;
                    }
                }
            }
        }
        
        else
        {
            var splitted = input.Split(' ');
            var command = splitted[0];
            var rate = Convert.ToDouble(splitted[^1]);
            var movie = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(" ", splitted[1..^1]));
            var movieId = "";
            foreach (var el in listMovies)
            {
                if (el.movie_title == movie)
                {
                    movieId = el.movie_id;
                    break;
                }
            }

            if (command == "rate")
            {
                if (movieId == "")
                {
                    Console.WriteLine("no such movie found :( closest candidates:");
                    spellChecker(movie);
                }
                else
                {
                    UserRating rating1 = new UserRating
                    {
                        _id = "0",
                        movie_id = movieId,
                        rating_val = (int)rate,
                        user_id = "user123"
                    };
                    new_user.Add(rating1);
                    Console.WriteLine($"> you've rated a film '{movie}' ({movieId}) as {rate}");

                    count++;
                    Console.WriteLine(count);

                    var movieGenre = listMovies.Find(m => m.movie_id == movieId)?.genres;
                    genreCounts[movieGenre]++;
                }
            }
        }
    }
}


void spellChecker(string movieTitle)
{
    var distances = new Dictionary<string, int>();
    var listTitles = listMovies.Select(movie1 => movie1.movie_title).ToList();

    foreach (var title in listTitles)
    {
        distances[title] = DamerauLevenshteinDistance(movieTitle, title);
    }

    var sortedKeyValuePairs = distances.OrderBy(x => x.Value).Take(3).ToList();
    var suggestions = sortedKeyValuePairs.Select(pair => pair.Key).ToList();

    Console.WriteLine(string.Join(", ", suggestions));
}


int DamerauLevenshteinDistance(string word1, string word2)
{
    var w1 = word1.Length;
    var w2 = word2.Length;

    var matrix = new int[w1 + 1, w2 + 1];

    for (var j = 0; j <= w2; j++)
        matrix[0, j] = j; // прописує рядок з індексами
    for (var i = 0; i <= w1; i++)
        matrix[i, 0] = i; // прописує стовбець з індексами

    for (var j = 1; j <= w2; j++)
    {
        for (var i = 1; i <= w1; i++)
        {
            var cost = word1[i - 1] == word2[j - 1] ? 0 : 1;

            matrix[i, j] = Math.Min(
                Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                matrix[i - 1, j - 1] + cost);

            if (i > 1 && j > 1 && word1[i - 1] == word2[j - 2] && word1[i - 2] == word2[j - 1])
            {
                matrix[i, j] = Math.Min(matrix[i, j], matrix[i - 2, j - 2] + cost);
            }
        }
    }
    return matrix[w1, w2];
}


public class Movie
{
    public string genres { get; set; }
    public string movie_title { get; set; }
    public string movie_id { get; set; }
    public string vote_average { get; set; }
}

public class UserRating
{
    public string _id { get; set; }
    public string movie_id { get; set; }
    public int rating_val { get; set; }
    public string user_id { get; set; }
}
