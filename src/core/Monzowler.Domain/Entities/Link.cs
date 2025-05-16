namespace Monzowler.Domain.Entities;

public class Link
{
    //Represents the Url we are going to crawl
    public string Url { get; set; }
    //The root main of the Url to be crawled
    public string Domain { get; set; }
    //Current depth of our Url 
    public int Depth { get; set; }
    //Retries on this Url if it failed because of timeout errors 
    public int Retries { get; set; }
}