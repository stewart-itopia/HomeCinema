﻿using System.Linq;
using AutoMapper;
using HomeCinema.Entities;
using HomeCinema.Web.Models;

namespace HomeCinema.Web.Mappings
{
    public class DomainToViewModelMappingProfile : Profile
    {
        public override string ProfileName
        {
            get { return "DomainToViewModelMappings"; }
        }

        protected override void Configure()
        {
            Mapper.CreateMap<Movie, MovieViewModel>()
                .ForMember(vm => vm.Genre, map => map.MapFrom(m => m.Genre.Name))
                .ForMember(vm => vm.GenreId, map => map.MapFrom(m => m.Genre.Id))
                .ForMember(vm => vm.IsAvailable, map => map.MapFrom(m => m.Stocks.Any(s => s.IsAvailable)))
                .ForMember(vm => vm.NumberOfStocks, map => map.MapFrom(m => m.Stocks.Count))
                .ForMember(vm => vm.Image,
                    map => map.MapFrom(m => string.IsNullOrEmpty(m.Image) ? "unknown.jpg" : m.Image));

            Mapper.CreateMap<Genre, GenreViewModel>()
                .ForMember(vm => vm.NumberOfMovies, map => map.MapFrom(g => g.Movies.Count()));
            // code omitted
            Mapper.CreateMap<Customer, CustomerViewModel>();

            Mapper.CreateMap<Stock, StockViewModel>();

            Mapper.CreateMap<Rental, RentalViewModel>();
        }
    }
}