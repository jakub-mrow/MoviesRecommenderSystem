using Microsoft.AspNetCore.Mvc;
namespace web_services_l1.Controllers;

[ApiController]
[Route("[controller]")]
public class RatingsController : ControllerBase
{
    [HttpPost("UploadRatingsCsv")]
    public string Post(IFormFile inputFile)
    {
        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer,0,(int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();
        MoviesContext dbContext = new MoviesContext();
        bool skip_header = true;
        int i = 0;
        foreach(string line in fileContent.Split('\n'))
        {
            try{
            if(skip_header){
                skip_header =false;
                continue;
            }
            var tokens = line.Split(",");
            if(tokens.Length != 4) continue;
            string UserID = tokens[0];
            string MovieID = tokens[1];
            string Rating = tokens[2];

            List<Rating> ratings = new List<Rating>();
            
            User u = new User();
            u.Name = "AAAA";
            u.UserID = (int)Int64.Parse(UserID);

            if(!dbContext.Users.Any(e=>e.UserID.Equals((int)Int64.Parse(UserID)))) {
                dbContext.Users.Add(u);
                dbContext.SaveChanges();
            }
            Rating r = new Rating();
            
            
            r.RatingValue = (int)int.Parse(Rating.Replace(".0", "").Replace(".5", ""));

            IQueryable<Movie> results = dbContext.Movies.Where(e => e.MovieID == int.Parse(MovieID));
            if(results.Count() > 0){
                r.RatedMovie = results.First();
            
            
                IQueryable<User> usersResults = dbContext.Users.Where(e => e.UserID == int.Parse(UserID));
                if(usersResults.Count() > 0){
                    r.RatingUser = usersResults.First();
                
                    dbContext.Ratings.Add(r);
                    dbContext.SaveChanges();
                }
            }
            // if(!dbContext.Ratings.Any(e => e.Name == g.Name))
            // {
            //     dbContext.Genres.Add(g);
            //     dbContext.SaveChanges();
            // }
            }
            catch(Exception e){
                Console.WriteLine(e);
            }
        }
        
        return "OK";
    }

}