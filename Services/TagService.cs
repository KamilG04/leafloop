using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class TagService : ITagService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TagService> _logger;

        public TagService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TagService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TagDto> GetTagByIdAsync(int id)
        {
            try
            {
                var tag = await _unitOfWork.Tags.GetByIdAsync(id);
                return _mapper.Map<TagDto>(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tag with ID: {TagId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
        {
            try
            {
                var tags = await _unitOfWork.Tags.GetAllAsync();
                return _mapper.Map<IEnumerable<TagDto>>(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all tags");
                throw;
            }
        }

        public async Task<IEnumerable<TagDto>> GetPopularTagsAsync(int count)
        {
            try
            {
                var tags = await _unitOfWork.Tags.GetPopularTagsAsync(count);
                return _mapper.Map<IEnumerable<TagDto>>(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting popular tags");
                throw;
            }
        }

        public async Task<IEnumerable<TagDto>> GetItemTagsAsync(int itemId)
        {
            try
            {
                var tags = await _unitOfWork.Tags.GetItemTagsAsync(itemId);
                return _mapper.Map<IEnumerable<TagDto>>(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tags for item: {ItemId}", itemId);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetItemsByTagAsync(int tagId, int page = 1, int pageSize = 10)
        {
            try
            {
                var items = await _unitOfWork.Items.GetItemsByTagAsync(tagId);
                
                // Apply pagination
                items = items.Skip((page - 1) * pageSize).Take(pageSize);
                
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items by tag: {TagId}", tagId);
                throw;
            }
        }

        public async Task<int> CreateTagAsync(TagCreateDto tagDto)
        {
            try
            {
                // Check if tag with the same name already exists
                var existingTag = await _unitOfWork.Tags.SingleOrDefaultAsync(t => t.Name.ToLower() == tagDto.Name.ToLower());
                
                if (existingTag != null)
                {
                    return existingTag.Id; // Return existing tag ID
                }
                
                var tag = _mapper.Map<Tag>(tagDto);
                
                await _unitOfWork.Tags.AddAsync(tag);
                await _unitOfWork.CompleteAsync();
                
                return tag.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating tag");
                throw;
            }
        }

        public async Task UpdateTagAsync(TagUpdateDto tagDto)
        {
            try
            {
                var tag = await _unitOfWork.Tags.GetByIdAsync(tagDto.Id);
                
                if (tag == null)
                {
                    throw new KeyNotFoundException($"Tag with ID {tagDto.Id} not found");
                }
                
                // Check if the tag is a system tag and if we're trying to change something critical
                if (tag.IsSystem && (tag.Name != tagDto.Name || tag.IsSystem != tagDto.IsSystem))
                {
                    throw new InvalidOperationException("Cannot modify system tag properties");
                }
                
                _mapper.Map(tagDto, tag);
                
                _unitOfWork.Tags.Update(tag);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating tag: {TagId}", tagDto.Id);
                throw;
            }
        }

        public async Task DeleteTagAsync(int id)
        {
            try
            {
                var tag = await _unitOfWork.Tags.GetByIdAsync(id);
                
                if (tag == null)
                {
                    throw new KeyNotFoundException($"Tag with ID {id} not found");
                }
                
                // Prevent deletion of system tags
                if (tag.IsSystem)
                {
                    throw new InvalidOperationException("Cannot delete system tags");
                }
                
                _unitOfWork.Tags.Remove(tag);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting tag: {TagId}", id);
                throw;
            }
        }
    }
}