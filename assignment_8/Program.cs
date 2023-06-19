using System.Globalization;
using CsvHelper;

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
    using var reader = new StreamReader(fileName);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    csv.Configuration.MissingFieldFound = null;
    var ratings = csv.GetRecords<UserRating>();
    return new List<UserRating>(ratings);
}

List<Movie> ArrangeMovies(List<Movie> listM)
{
    foreach (var el in listM)
    {
        if (el.vote_average == "")
        {
            el.vote_average = "0";
        }
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

Dictionary<string, List<Movie>> GetFilmsWatchedByUser(List<UserRating> userRatings, List<Movie> listMovies)
{
    Dictionary<string, List<Movie>> filmsWatchedByUser = new Dictionary<string, List<Movie>>();

    foreach (var rating in userRatings)
    {
        if (!filmsWatchedByUser.TryGetValue(rating.user_id, out var movies))
        {
            movies = new List<Movie>();
            filmsWatchedByUser[rating.user_id] = movies;
        }

        var movie = listMovies.FirstOrDefault(m => m.movie_id == rating.movie_id);
        if (movie != null)
        {
            movies.Add(movie);
        }
    }

    return filmsWatchedByUser;
}

void FindNotWatched(string otherUser, Dictionary<string, List<Movie>> usersWatched, List<Movie> ourUser, string favGenre)
{
    if (usersWatched.TryGetValue(otherUser, out var u))
    {
        var toWatch = u.Except(ourUser).ToList();
        
        Console.WriteLine($"as you like {favGenre}, we might suggest");
        foreach (var movie in toWatch.Where(movie => movie.genres == favGenre))
        {
            Console.WriteLine($"{movie.movie_title}");
        }
        Console.WriteLine();
    
        Console.WriteLine("from other genres you may like");
        var others = toWatch.Where(movie => movie.genres != favGenre).ToList();
        foreach (var movie in others.Take(5))
        {
            Console.WriteLine($"{movie.movie_title}");
        }
    }
}


var listMovies = ArrangeMovies(GetMovies("movie_data.csv"));
var userRatings = GetUserRatings("rating.csv");

var userPreferences = GetUserPreferences(userRatings, listMovies);
var dictOfBests = BestFromEachGenre(listMovies);
var watchedByUsers = GetFilmsWatchedByUser(userRatings, listMovies);

var a = new KdTree();
a.BuildTree(userPreferences);

Recommend();

void Recommend()
{
    var genres = new List<string>{"Drama", "Comedy", "Documentary", "Fiction", "Action",
        "Romance", "Animation", "Thriller"};
    List<UserRating> new_user = new List<UserRating>();
    int count = 0;
    var genreCounts = new Dictionary<string, int>(); // Хранит количество просмотренных фильмов каждого жанра
    var watched = new List<Movie>();
    foreach (var g in genres) genreCounts[g] = 0;
    
    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (input == "recommend")
        {
            if (count > 2)
            {
                var prefs = new double[8];
                var new_userPreferences = GetUserPreferences(new_user, listMovies);
                foreach (var userId in new_userPreferences.Keys)
                {
                    //Console.WriteLine(userId + ": [" + string.Join(", ", new_userPreferences[userId]) + "]");
                    prefs = new_userPreferences[userId];
                }
                var s = a.FindNearest(prefs);
            
                var favoriteGenre = GetFavoriteGenre(genreCounts);
                FindNotWatched(s[0].Key, watchedByUsers, watched, favoriteGenre);
                
                break;
            }
            Console.WriteLine($"you need to rate {2-count} more movies to open this option");
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
                        var rate = double.Parse(Console.ReadLine().Replace(',', '.'), CultureInfo.InvariantCulture);
                        if (rate > 10)
                        {
                            Console.WriteLine("invalid rate");
                            rate = double.Parse(Console.ReadLine().Replace(',', '.'), CultureInfo.InvariantCulture);
                        }
                        else
                        {
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
                            watched.Add(movie);
                        }
                    }
                }
            }
        }
        
        else
        {
            var splitted = input.Split(' ');
            var command = splitted[0];

            if (command == "rate")
            {
                var rate = double.Parse(splitted[^1].Replace(',', '.'), CultureInfo.InvariantCulture);
                if (rate > 10)
                {
                    Console.WriteLine("invalid rate");
                    rate = double.Parse(Console.ReadLine().Replace(',', '.'), CultureInfo.InvariantCulture);
                }
                
                var movie = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(" ", splitted[1..^1]));
                var movieId = "";
                foreach (var el in listMovies)
                {
                    if (el.movie_title == movie)
                    {
                        movieId = el.movie_id;
                        watched.Add(el);
                        break;
                    }
                }
                if (movieId == "")
                {
                    Console.WriteLine("no such movie found :( closest candidates:");
                    SpellChecker(movie);
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

                    var movieGenre = listMovies.Find(m => m.movie_id == movieId)?.genres;
                    genreCounts[movieGenre]++;
                }
            }
        }
    }
}

void SpellChecker(string movieTitle)
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

public class Node
{
    public double[] Preferences { get; set; }
    public Node LeftChild { get; set; }
    public Node RightChild { get; set; }
    public string User { get; set; }

    public Node(string user, double[] preferences)
    {
        User = user;
        Preferences = preferences;
        LeftChild = null;
        RightChild = null;
    }
}

public class KdTree
{
    private Node Root { get; set; }
    private const int Dimensions = 8;

    public void BuildTree(Dictionary<string, double[]> preferences)
    {
        Root = BuildTreeRecursive(preferences, 0);

    }

    private Node BuildTreeRecursive(Dictionary<string, double[]> preferences, int depth)
    {
        if (preferences.Count == 0)
            return null;

        int axis = depth % Dimensions;
        var sortedPreferences = preferences.OrderBy
            (p => p.Value[axis]).ToList();

        int medianIndex = sortedPreferences.Count / 2;
        KeyValuePair<string, double[]> medianUser = sortedPreferences[medianIndex];
        double[] medianPreferences = medianUser.Value;

        Node node = new Node(medianUser.Key, medianPreferences)
        {
            LeftChild = BuildTreeRecursive(sortedPreferences
                .GetRange(0, medianIndex).ToDictionary(x => x.Key,
                    x => x.Value), depth + 1),
            RightChild = BuildTreeRecursive(sortedPreferences
                .GetRange(medianIndex + 1, sortedPreferences.Count - (medianIndex + 1))
                .ToDictionary(x => x.Key, x => x.Value), depth + 1)
        };

        return node;
    }


    public List<KeyValuePair<string, double[]>> FindNearest(double[] targetPreferences)
    {
        int k = 2; // how many neighbors we want to find
        List<KeyValuePair<string, double[]>> nearestNeighbors = new List<KeyValuePair<string, double[]>>();
        FindNearestRecursive(Root, targetPreferences, k, 0, nearestNeighbors);
        return nearestNeighbors;
    }


    private void FindNearestRecursive(Node node, double[] targetPreferences, int k, int depth,
        List<KeyValuePair<string, double[]>> nearestNeighbors)
    {
        if (node == null)
            return;

        int axis = depth % Dimensions;
        double distance = CalculateDistance(node.Preferences, targetPreferences);

        if (nearestNeighbors.Count < k)
        {
            nearestNeighbors.Add(new KeyValuePair<string, double[]>(node.User, node.Preferences));
        }
        else if (distance < CalculateDistance(targetPreferences, nearestNeighbors.Last().Value))
        {
            nearestNeighbors.RemoveAt(nearestNeighbors.Count - 1);
            nearestNeighbors.Add(new KeyValuePair<string, double[]>(node.User, node.Preferences));
        }
        else
        {
            return;
        }

        if (targetPreferences[axis] < node.Preferences[axis])
        {
            FindNearestRecursive(node.LeftChild, targetPreferences, k, depth + 1, nearestNeighbors);
            if (CalculateDistance(targetPreferences, nearestNeighbors.Last().Value) >
                Math.Abs(targetPreferences[axis] - node.Preferences[axis]))
                FindNearestRecursive(node.RightChild, targetPreferences, k, depth + 1, nearestNeighbors);
        }
        else
        {
            FindNearestRecursive(node.RightChild, targetPreferences, k, depth + 1, nearestNeighbors);
            if (CalculateDistance(targetPreferences, nearestNeighbors.Last().Value) >
                Math.Abs(targetPreferences[axis] - node.Preferences[axis]))
                FindNearestRecursive(node.LeftChild, targetPreferences, k, depth + 1, nearestNeighbors);
        }
    }

    private double CalculateDistance(double[] preferences1, double[] preferences2)
    {
        double distance = 0;
        for (int i = 0; i < preferences1.Length; i++)
        {
            double diff = preferences1[i] - preferences2[i];
            distance += diff * diff;
        }

        return Math.Sqrt(distance);
    }
}