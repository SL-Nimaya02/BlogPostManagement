using Microsoft.EntityFrameworkCore;
using BlogPostManagement.Models;

namespace BlogPostManagement.Context
{
    public class MyDataContext:DbContext
    {
        public MyDataContext(DbContextOptions<MyDataContext> options) : base(options) 
        { 
        }
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Category> Categories => Set<Category>();
    }
}
