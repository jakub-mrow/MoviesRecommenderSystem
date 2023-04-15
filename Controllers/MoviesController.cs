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

}