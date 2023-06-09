using Microsoft.AspNetCore.Mvc;
namespace web_services_l1.Controllers;
[ApiController]
[Route("[controller]")]
public class MoviesController : ControllerBase
{
    [HttpPost("UploadMovieCsv")]
    public string Post(IFormFile inputFile)
    {

        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();

        MoviesContext dbContext = new MoviesContext();

        bool skip_header = true;
        foreach (string line in fileContent.Split('\n'))
        {
            if (skip_header)
            {
                skip_header = false;
                continue;

            }
            var tokens = line.Split(",");
            if (tokens.Length != 3) continue;
            string MovieID = tokens[0];
            string MovieName = tokens[1];
            string[] Genres = tokens[2].Split("|");
            List<Genre> movieGenres = new List<Genre>();
            foreach (string genre in Genres)
            {
                Genre g = new Genre();
                g.Name = genre;
                if (!dbContext.Genres.Any(e => e.Name == g.Name))
                {
                    dbContext.Genres.Add(g);
                    dbContext.SaveChanges();
                }
                IQueryable<Genre> results = dbContext.Genres.Where(e => e.Name == g.Name);
                if (results.Count() > 0)
                    movieGenres.Add(results.First());
            }
            Movie m = new Movie();
            m.MovieID = int.Parse(MovieID);
            m.Title = MovieName;
            m.Genres = movieGenres;
            if (!dbContext.Movies.Any(e => e.MovieID == m.MovieID)) dbContext.Movies.Add(m);
            dbContext.SaveChanges();
        }
        dbContext.SaveChanges();



        return "OK";
    }



    [HttpPost("UploadRatingsCsv")]
    public string UploadRatingsCsv(IFormFile inputFile)
    {

        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();

        MoviesContext dbContext = new MoviesContext();

        bool skip_header = true;
        int id = 0;
        foreach (string line in fileContent.Split('\n'))
        {
            if (skip_header)
            {
                skip_header = false;
                continue;

            }
            var tokens = line.Split(",");
            if (tokens.Length != 4) continue;
            string UserId = tokens[0];
            string MovieId = tokens[1];
            string Rating = tokens[2];
            List<Genre> movieGenres = new List<Genre>();




        }
        dbContext.SaveChanges();



        return "OK";
    }



    [HttpGet("GetAllGenres")]
    public IEnumerable<Genre> GetAllGenres()
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Genres.AsEnumerable();
    }

    [HttpGet("GetMoviesByName/{search_phrase}")]
    public IEnumerable<Movie> GetMoviesByName(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(e => e.Title.Contains(search_phrase));
    }


    [HttpPost("GetMoviesByGenre")]
    public IEnumerable<Movie> GetMoviesByGenre(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(
        m => m.Genres.Any(p => p.Name.Contains(search_phrase))
        );
    }

    [HttpGet("GetGenresByMovieId/{movie_id}")]
    public IEnumerable<Genre> getGenresByMovieId(int movie_id)
    {
        MoviesContext dbContext = new MoviesContext();

        return dbContext.Genres.Where(m => m.Movies.Any(p => p.MovieID.Equals(movie_id)));
    }


    [HttpGet("genresVector/{movie_id}")]
    public int[] genresVector(int movie_id)
    {
        int[] vector = new int[37];
        
        using (var dbContext = new MoviesContext())
        {
            IEnumerable<Genre> movieGenres = dbContext.Genres.Where(m => m.Movies.Any(p => p.MovieID.Equals(movie_id))).ToList();

            IEnumerable<Genre> allGenres = dbContext.Genres.AsEnumerable();

            int i = 0;
            foreach(Genre genre in allGenres){
                bool allProcessed = true;
                foreach(Genre movieGenre in movieGenres){
                    if (genre.Name == movieGenre.Name){
                        vector[i] = 1;
                        allProcessed = false;
                        break;
                    } 
                }
                if (allProcessed){
                    vector[i] = 0;
                }
                i++;
                if (i == 37){
                    break;
                }
            }
        }
        return vector;
    }


    [HttpGet("compareMovies/{movie1_id}/{movie2_id}")]
    public double compareMovies(int movie1_id, int movie2_id){

        int[] vector1 = genresVector(movie1_id);
        int[] vector2 = genresVector(movie2_id);

        return CosineSimilarity(vector1, vector2);
    }


    [HttpGet("getMoviesWithSimilarGenres/{movie_id}")]
    public IEnumerable<Movie> getMoviesWithSimilarGenres(int movie_id){

        List<Movie> similarMovies = new List<Movie>();
        using (var dbContext = new MoviesContext()){
            IEnumerable<Genre> movieGenres = dbContext.Genres.Where(m => m.Movies.Any(p => p.MovieID.Equals(movie_id))).ToList();

            IEnumerable<Movie> allMovies = dbContext.Movies.AsEnumerable().ToList();

        
            foreach(Movie movie in allMovies){
                IEnumerable<Genre> genres = dbContext.Genres.Where(m => m.Movies.Any(p => p.MovieID.Equals(movie.MovieID)));
                foreach(Genre movieGenre in genres){
                    bool genreExists = movieGenres.Any(genre => genre.Name == movieGenre.Name);
                    if (genreExists){
                        similarMovies.Add(movie);
                        break;
                    }
                }
            }
        }
        return similarMovies;
    }


    [HttpGet("getMoviesAboveThreshold/{movie_id}/{threshold}")]
    public IEnumerable<Movie> getMoviesAboveThreshold(int movie_id, double threshold){
        List<Movie> similarMovies = new List<Movie>();
        using (var dbContext = new MoviesContext()){
            IEnumerable<Movie> allMovies = dbContext.Movies.AsEnumerable().ToList();

            foreach(Movie movie in allMovies){
                int[] vector1 = genresVector(movie_id);
                int[] vector2 = genresVector(movie.MovieID);
                double moviesSimilarity = CosineSimilarity(vector1, vector2);
                if (moviesSimilarity >= threshold && movie_id != movie.MovieID){
                    similarMovies.Add(movie);
                }
            }
        }
        return similarMovies;
    }


    [HttpGet("getSortedUserRatedMovies/{user_id}")]
    public IEnumerable<RatedMovie> getSortedUserRatedMovies(int user_id){
        List<RatedMovie> ratedMovies = new List<RatedMovie>();
        using (var dbContext = new MoviesContext()){
            IEnumerable<Movie> allMovies = dbContext.Movies.AsEnumerable().ToList();
            IEnumerable<Rating> userRatings = dbContext.Ratings.Where(r => r.RatingUser.UserID == user_id).OrderByDescending(r => r.RatingValue).ToList();
            foreach(Rating rating in userRatings){
                foreach(Movie movie in allMovies){
                    if (rating.RatedMovie.MovieID == movie.MovieID){
                        ratedMovies.Add(new RatedMovie { Movie = movie, RatingValue = rating.RatingValue });
                    }
                }
            }
        }

        return ratedMovies;
    }


    [HttpGet("getUserRatedMovies/{user_id}")]
    public IEnumerable<Movie> getUserRatedMovies(int user_id){
        List<Movie> ratedMovies = new List<Movie>();
        using (var dbContext = new MoviesContext()){
            IEnumerable<Movie> allMovies = dbContext.Movies.AsEnumerable().ToList();
            IEnumerable<Rating> userRatings = dbContext.Ratings.Where(r => r.RatingUser.UserID == user_id).ToList();
            foreach(Rating rating in userRatings){
                foreach(Movie movie in allMovies){
                    if (rating.RatedMovie.MovieID == movie.MovieID){
                        ratedMovies.Add(movie);
                    }
                }
            }
        }

        return ratedMovies;
    }

    [HttpGet("getMostSimilarMovies/{user_id}")]
    public IEnumerable<Movie> getMostSimilarMovies(int user_id){
        IEnumerable<Movie> emptyMovies = Enumerable.Empty<Movie>();
        using (var dbContext = new MoviesContext()){
            IEnumerable<Movie> allMovies = dbContext.Movies.AsEnumerable().ToList();
            Rating highestRating = dbContext.Ratings.Where(r => r.RatingUser.UserID == user_id).OrderByDescending(r => r.RatingValue).ToList().First();
            foreach(Movie movie in allMovies){
                if (highestRating.RatedMovie.MovieID == movie.MovieID){
                    IEnumerable<Movie> similarMovies = getMoviesWithSimilarGenres(movie.MovieID);
                    return similarMovies;
                }
            }
            
        }
        return emptyMovies;
    }


    [HttpGet("getSetMostSimilarMovies/{user_id}/{size}")]
    public IEnumerable<Movie> getSetMostSimilarMovies(int user_id, int size){
        IEnumerable<Movie> emptyMovies = Enumerable.Empty<Movie>();
        using (var dbContext = new MoviesContext()){
            IEnumerable<Movie> allMovies = dbContext.Movies.AsEnumerable().ToList();
            Rating highestRating = dbContext.Ratings.Where(r => r.RatingUser.UserID == user_id).OrderByDescending(r => r.RatingValue).ToList().First();
            foreach(Movie movie in allMovies){
                if (highestRating.RatedMovie.MovieID == movie.MovieID){
                    IEnumerable<Movie> similarMovies = getMoviesWithSimilarGenres(movie.MovieID).Take(size);
                    return similarMovies;
                }
            }
            
        }
        return emptyMovies;
    }



    static double CosineSimilarity(int[] vector1, int[] vector2)
    {
        double dotProduct = vector1.Select((x, i) => x * vector2[i]).Sum();
        double magnitude1 = Math.Sqrt(vector1.Select(x => x * x).Sum());
        double magnitude2 = Math.Sqrt(vector2.Select(x => x * x).Sum());

        return dotProduct / (magnitude1 * magnitude2);
    }

    public class RatedMovie
    {
        public Movie Movie { get; set; }
        public int RatingValue { get; set; }
    }

}


