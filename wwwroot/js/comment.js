$(document).ready(function () {
    // Handle comment form submission
    $('.comment-form').on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        var formData = new FormData(this);

        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success === false) {
                    // Show error message
                    alert(response.message);
                } else {
                    // Add the new comment to the comments list
                    $('.comments-list').prepend(response.html);
                    
                    // Clear the form
                    form.find('textarea').val('');
                    form.find('input[type="file"]').val('');
                    form.find('.image-preview').empty();
                    
                    // Update comment count
                    $('.comment-count').text(response.commentCount);

                    // Scroll to the new comment
                    $('html, body').animate({
                        scrollTop: $('#comment-' + response.commentId).offset().top - 100
                    }, 1000);

                    // Highlight the new comment briefly
                    $('#comment-' + response.commentId).addClass('highlight');
                    setTimeout(function() {
                        $('#comment-' + response.commentId).removeClass('highlight');
                    }, 3000);
                }
            },
            error: function () {
                alert('Có lỗi xảy ra khi gửi bình luận');
            }
        });
    });

    // Handle reply form submission
    $('.nested-reply-form').on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        var formData = new FormData(this);
        var parentCommentId = form.find('input[name="ParentCommentId"]').val();

        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success === false) {
                    // Show error message
                    alert(response.message);
                } else {
                    // Hide the reply form
                    form.closest('.reply-form').hide();
                    
                    // Clear the form
                    form.find('textarea').val('');
                    form.find('input[type="file"]').val('');
                    form.find('.image-preview').empty();
                    
                    // Add the new reply to the replies list
                    var repliesContainer = form.closest('.comment-item').find('.nested-replies');
                    if (repliesContainer.length === 0) {
                        repliesContainer = $('<div class="nested-replies"></div>');
                        form.closest('.comment-item').append(repliesContainer);
                    }
                    repliesContainer.append(response.html);

                    // Update comment count
                    $('.comment-count').text(response.commentCount);

                    // Scroll to the new reply
                    $('html, body').animate({
                        scrollTop: $('#comment-' + response.commentId).offset().top - 100
                    }, 1000);

                    // Highlight the new reply briefly
                    $('#comment-' + response.commentId).addClass('highlight');
                    setTimeout(function() {
                        $('#comment-' + response.commentId).removeClass('highlight');
                    }, 3000);
                }
            },
            error: function () {
                alert('Có lỗi xảy ra khi gửi phản hồi');
            }
        });
    });

    // Handle image preview
    $('input[type="file"]').change(function () {
        var input = this;
        var preview = $(this).siblings('.image-preview');
        
        if (input.files && input.files[0]) {
            var reader = new FileReader();
            
            reader.onload = function (e) {
                preview.html('<img src="' + e.target.result + '" class="img-fluid mt-2" style="max-width: 200px;">');
            }
            
            reader.readAsDataURL(input.files[0]);
        } else {
            preview.empty();
        }
    });
}); 