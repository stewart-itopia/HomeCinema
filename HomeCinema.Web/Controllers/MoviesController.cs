﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using HomeCinema.Data.Infrastructure;
using HomeCinema.Data.Repositories;
using HomeCinema.Entities;
using HomeCinema.Web.Infrastructure.Core;
using HomeCinema.Web.Infrastructure.Extensions;
using HomeCinema.Web.Models;

namespace HomeCinema.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [RoutePrefix("api/movies")]
    public class MoviesController : ApiControllerBase
    {
        private readonly IEntityBaseRepository<Movie> _moviesRepository;

        public MoviesController(IEntityBaseRepository<Movie> moviesRepository,
            IEntityBaseRepository<Error> _errorsRepository, IUnitOfWork _unitOfWork)
            : base(_errorsRepository, _unitOfWork)
        {
            _moviesRepository = moviesRepository;
        }

        [AllowAnonymous]
        [Route("latest")]
        public HttpResponseMessage Get(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var movies = _moviesRepository.GetAll().OrderByDescending(m => m.ReleaseDate).Take(6).ToList();

                var moviesVM =
                    Mapper.Map<IEnumerable<Movie>, IEnumerable<MovieViewModel>>(movies);

                response = request.CreateResponse(HttpStatusCode.OK, moviesVM);

                return response;
            });
        }

        [Route("details/{id:int}")]
        public HttpResponseMessage Get(HttpRequestMessage request, int id)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var movie = _moviesRepository.GetSingle(id);

                var movieVM = Mapper.Map<Movie, MovieViewModel>(movie);

                response = request.CreateResponse(HttpStatusCode.OK, movieVM);

                return response;
            });
        }

        [AllowAnonymous]
        [Route("{page:int=0}/{pageSize=3}/{filter?}")]
        public HttpResponseMessage Get(HttpRequestMessage request, int? page, int? pageSize, string filter = null)
        {
            var currentPage = page.Value;
            var currentPageSize = pageSize.Value;

            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                List<Movie> movies = null;
                var totalMovies = new int();

                if (!string.IsNullOrEmpty(filter))
                {
                    movies = _moviesRepository.GetAll()
                        .OrderBy(m => m.Id)
                        .Where(m => m.Title.ToLower()
                            .Contains(filter.ToLower().Trim()))
                        .ToList();
                }
                else
                {
                    movies = _moviesRepository.GetAll().ToList();
                }

                totalMovies = movies.Count();
                movies = movies.Skip(currentPage*currentPageSize)
                    .Take(currentPageSize)
                    .ToList();

                var moviesVM = Mapper.Map<IEnumerable<Movie>, IEnumerable<MovieViewModel>>(movies);

                var pagedSet = new PaginationSet<MovieViewModel>
                {
                    Page = currentPage,
                    TotalCount = totalMovies,
                    TotalPages = (int) Math.Ceiling((decimal) totalMovies/currentPageSize),
                    Items = moviesVM
                };

                response = request.CreateResponse(HttpStatusCode.OK, pagedSet);

                return response;
            });
        }

        [MimeMultipart]
        [Route("images/upload")]
        public HttpResponseMessage Post(HttpRequestMessage request, int movieId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                var movieOld = _moviesRepository.GetSingle(movieId);
                if (movieOld == null)
                    response = request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid movie.");
                else
                {
                    var uploadPath = HttpContext.Current.Server.MapPath("~/Content/images/movies");

                    var multipartFormDataStreamProvider = new UploadMultipartFormProvider(uploadPath);

                    // Read the MIME multipart asynchronously 
                    Request.Content.ReadAsMultipartAsync(multipartFormDataStreamProvider);

                    var _localFileName = multipartFormDataStreamProvider
                        .FileData.Select(multiPartData => multiPartData.LocalFileName).FirstOrDefault();

                    // Create response
                    var fileUploadResult = new FileUploadResult
                    {
                        LocalFilePath = _localFileName,
                        FileName = Path.GetFileName(_localFileName),
                        FileLength = new FileInfo(_localFileName).Length
                    };

                    // update database
                    movieOld.Image = fileUploadResult.FileName;
                    _moviesRepository.Edit(movieOld);
                    _unitOfWork.Commit();

                    response = request.CreateResponse(HttpStatusCode.OK, fileUploadResult);
                }

                return response;
            });
        }

        [HttpPost]
        [Route("update")]
        public HttpResponseMessage Update(HttpRequestMessage request, MovieViewModel movie)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                if (!ModelState.IsValid)
                {
                    response = request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
                else
                {
                    var movieDb = _moviesRepository.GetSingle(movie.ID);
                    if (movieDb == null)
                        response = request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid movie.");
                    else
                    {
                        movieDb.UpdateMovie(movie);
                        movie.Image = movieDb.Image;
                        _moviesRepository.Edit(movieDb);

                        _unitOfWork.Commit();
                        response = request.CreateResponse(HttpStatusCode.OK, movie);
                    }
                }

                return response;
            });
        }

        [HttpPost]
        [Route("add")]
        public HttpResponseMessage Add(HttpRequestMessage request, MovieViewModel movie)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                if (!ModelState.IsValid)
                {
                    response = request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
                else
                {
                    var newMovie = new Movie();
                    newMovie.UpdateMovie(movie);

                    for (var i = 0; i < movie.NumberOfStocks; i++)
                    {
                        var stock = new Stock
                        {
                            IsAvailable = true,
                            Movie = newMovie,
                            UniqueKey = Guid.NewGuid()
                        };
                        newMovie.Stocks.Add(stock);
                    }

                    _moviesRepository.Add(newMovie);

                    _unitOfWork.Commit();

                    // Update view model
                    movie = Mapper.Map<Movie, MovieViewModel>(newMovie);
                    response = request.CreateResponse(HttpStatusCode.Created, movie);
                }

                return response;
            });
        }
    }
}