using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MapOfActivitiesAPI.Models;
using System.Reflection.Emit;

namespace MapOfActivitiesAPI.Models
{
    public class MapOfActivitiesAPIContext : DbContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Type> Types { get; set; }
        public DbSet<Visitings> Visitings { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Complaint> Complaints { get; set; }

        public DbSet<WebLog> WebLogs { get; set; }

        public MapOfActivitiesAPIContext(DbContextOptions<MapOfActivitiesAPIContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Event>(entity =>
            {
                entity.HasOne(e => e.Type)
                .WithMany(e => e.Events)
                .HasForeignKey(e => e.TypeId)
                .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<Event>(entity =>
            {
                entity.HasOne(e => e.User)
                .WithMany(e => e.CreatedEvents)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public DbSet<MapOfActivitiesAPI.Models.EventView>? EventView { get; set; }
    }
}
