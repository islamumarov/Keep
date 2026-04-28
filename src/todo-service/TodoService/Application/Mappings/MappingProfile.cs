using AutoMapper;
using TodoService.Domain.Entities;
using TodoService.Application.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<TodoItem, TodoItemDto>();
        CreateMap<TodoItem, TodoItemSummaryDto>();
    }
}
