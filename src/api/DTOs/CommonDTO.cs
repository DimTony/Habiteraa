
namespace Habitera.DTOs
{

	public class OperationResult<T>
	{
		public bool Success { get; set; }
		public int StatusCode { get; set; }
		public string Message { get; set; } = string.Empty;
		public T? Data { get; set; }
		public object? Error { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;

		public static OperationResult<T> Successful(T? data, string message = "Success", int statusCode = 200)
		{
			return new OperationResult<T>
			{
				Success = true,
				StatusCode = statusCode,
				Message = message,
				Data = data,
				Error = null
			};
		}

		public static OperationResult<T> Failure(string message, int statusCode = 400, object? error = null)
		{
			return new OperationResult<T>
			{
				Success = false,
				StatusCode = statusCode,
				Message = message,
				Data = default(T),
				Error = error
			};
		}
	}

	public class PaginatedRequest
	{
		public int PageNumber { get; set; } = 1;     
		public int PageSize { get; set; } = 10;      
		public string? SearchWord { get; set; }      
	}

	public class PaginatedOperationResult<T>
	{
		public bool Success { get; set; }
		public int StatusCode { get; set; }
		public string Message { get; set; } = string.Empty;
		public IEnumerable<T>? Data { get; set; }  
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalRecords { get; set; }
		public int TotalPages { get; set; }
		public bool HasNextPage => PageNumber < TotalPages;
		public bool HasPreviousPage => PageNumber > 1;
		public object? Error { get; set; }

		public static PaginatedOperationResult<T> Successful(
			IEnumerable<T> data,
			int count,
			int pageNumber,
			int pageSize,
			string message = "Success",
			int statusCode = 200)
		{
			return new PaginatedOperationResult<T>
			{
				Success = true,
				StatusCode = statusCode,
				Message = message,
				Data = data,
				TotalRecords = count,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalPages = (int)Math.Ceiling(count / (double)pageSize),
			};
		}

		public static PaginatedOperationResult<T> Failure(
			string message,
			int statusCode = 400,
			object? error = null)
		{
			return new PaginatedOperationResult<T>
			{
				Success = false,
				StatusCode = statusCode,
				Message = message,
				Data = default(IEnumerable<T>),
				TotalRecords = 0,
				PageNumber = 0,
				PageSize = 0,
				TotalPages = 0,
				Error = error
			};
		}
	}
}