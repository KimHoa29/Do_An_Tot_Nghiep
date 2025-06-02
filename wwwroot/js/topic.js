window.TopicManager = {
    toggleReplyForm: function(commentId) {
        var form = document.getElementById('topic-reply-form-' + commentId);
        if (form) {
            form.style.display = (form.style.display === 'none' || form.style.display === '') ? 'block' : 'none';
            if (form.style.display === 'block') {
                var textarea = form.querySelector('textarea');
                if (textarea) textarea.focus();
            }
        }
    },
    submitReply: async function(event, parentCommentId) {
        event.preventDefault();
        const form = event.target;
        const formData = new FormData(form);
        const topicId = form.querySelector('input[name="TopicId"]').value;
        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        formData.append("ParentCommentId", parentCommentId);
        formData.append("TopicId", topicId);

        try {
            const response = await fetch('/Comments/CreateResponse', {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const result = await response.json();
            if (result.success) {
                let repliesContainer = document.getElementById('topic-replies-' + parentCommentId);
                if (!repliesContainer) {
                    const parentElement = document.getElementById('topic-reply-' + parentCommentId) || document.getElementById('topic-comment-' + parentCommentId);
                    if (parentElement) {
                        const replyContent = parentElement.querySelector('.reply-content') || parentElement.querySelector('.comment-body');
                        if (replyContent) {
                            repliesContainer = document.createElement('div');
                            repliesContainer.className = 'nested-replies';
                            repliesContainer.id = 'topic-replies-' + parentCommentId;
                            replyContent.appendChild(repliesContainer);
                        }
                    }
                }

                let level = 1;
                const parentReply = document.getElementById('topic-reply-' + parentCommentId);
                if (parentReply) {
                    level = 2;
                }

                if (repliesContainer) {
                    repliesContainer.insertAdjacentHTML('beforeend', TopicManager.createReplyElement(result.reply, topicId, level));
                }

                form.reset();
                TopicManager.toggleReplyForm(parentCommentId);

            } else {
                alert(result.message || 'Có lỗi xảy ra khi thêm phản hồi');
            }
        } catch (error) {
            console.error(error);
            alert('Có lỗi xảy ra khi thêm phản hồi');
        }
    },
    createReplyElement: function(reply, topicId, level = 1) {
        const globalToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const levelClass = level === 1 ? '' : 'reply-level-2';
        return `
            <div class="reply-item ${levelClass}" id="topic-reply-${reply.id}">
                <div class="reply-line"></div>
                <div class="d-flex">
                    <img src="${reply.avatar}" alt="Avatar" class="reply-avatar" />
                    <div class="reply-content">
                        <div class="reply-header">
                            <strong class="reply-username">
                                ${reply.username} –
                                <span class="role">${TopicManager.getRoleText(reply.role)}</span>
                            </strong>
                            <small class="reply-time">${reply.createdAt}</small>
                        </div>
                        <div class="reply-text">
                            ${reply.content}
                            ${reply.imageUrl ? `<div class='reply-image'><img src='${reply.imageUrl}' alt='Ảnh phản hồi' /></div>` : ''}
                        </div>
                        <div class="reply-actions">
                            <button class="custom-reply-btn" data-comment-id="${reply.id}">
                                <i class="fas fa-reply"></i> Trả lời
                            </button>
                        </div>
                        <div class="reply-form mt-2" id="topic-reply-form-${reply.id}" style="display: none;">
                            <form class="topic-nested-reply-form" onsubmit="TopicManager.submitReply(event, ${reply.id})" enctype="multipart/form-data">
                                <input type="hidden" name="ParentCommentId" value="${reply.id}" />
                                <input type="hidden" name="TopicId" value="${topicId}" />
                                <input type="hidden" name="__RequestVerificationToken" value="${globalToken}" />
                                <div class="mb-2">
                                    <textarea name="Content" class="form-control" placeholder="Nhập phản hồi..." rows="2" required></textarea>
                                </div>
                                <div class="mb-2">
                                    <input type="file" name="ImageUpload" class="form-control" accept="image/*" />
                                </div>
                                <div class="d-flex justify-content-end">
                                    <button type="button" class="btn btn-light me-2" onclick="TopicManager.toggleReplyForm('${reply.id}')">Hủy</button>
                                    <button type="submit" class="btn btn-purple">Gửi phản hồi</button>
                                </div>
                            </form>
                        </div>
                        <div class="nested-replies" id="topic-replies-${reply.id}"></div>
                    </div>
                </div>
            </div>
        `;
    },
    getRoleText: function(role) {
        switch (role) {
            case 'Lecturer': return 'Giảng viên, CVHT';
            case 'Admin': return 'Nhân viên';
            case 'Student': return 'Sinh viên';
            default: return 'Người dùng';
        }
    },
    toggleCommentBlock: function(topicId) {
        const block = document.getElementById(`comment-block-${topicId}`);
        if (!block) return;
        const wasHidden = block.style.display === 'none' || block.style.display === '';
        block.style.display = wasHidden ? 'block' : 'none';
        if (wasHidden) {
            const textarea = block.querySelector('textarea');
            if (textarea) textarea.focus();
        }
    },
    toggleMenu: function(topicId, event) {
        if (event) event.stopPropagation();
        const menu = document.getElementById(`topic-menu-${topicId}`);
        if (!menu) return;
        const isVisible = menu.classList.contains('show');
        document.querySelectorAll('.topic-menu').forEach(m => m.classList.remove('show'));
        if (!isVisible) menu.classList.add('show');
    },
    showDeleteTopicModal: function(topicId) {
        window.topicIdToDelete = topicId;
        const modal = new bootstrap.Modal(document.getElementById('deleteTopicModal'));
        modal.show();
    },
    toggleLike: function(topicId) {
        const likeBtn = document.querySelector(`.like-btn[data-topic-id="${topicId}"]`);
        const likeCountSpan = likeBtn.querySelector('.count-number');
        fetch(`/Topics/ToggleLike/${topicId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (data.isLiked) {
                    likeBtn.classList.add('liked');
                } else {
                    likeBtn.classList.remove('liked');
                }
                likeCountSpan.textContent = data.likeCount;
            } else {
                alert(data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Có lỗi xảy ra khi thực hiện thao tác. Vui lòng thử lại sau.');
        });
    },
    toggleSave: function(id, type) {
        const saveBtn = document.querySelector(`.save-btn[data-${type}-id="${id}"]`);
        const saveCountSpan = saveBtn.querySelector('.count-number');
        fetch(`/Saves/ToggleSave/${type}/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            credentials: 'same-origin'
        })
        .then(response => response.json())
        .then(data => {
            if (data.needLogin) {
                window.location.href = '/Account/Login';
                return;
            }
            if (data.success) {
                if (data.isSaved) {
                    saveBtn.classList.add('saved');
                } else {
                    saveBtn.classList.remove('saved');
                }
                saveCountSpan.textContent = data.saveCount;
            } else {
                alert(data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Có lỗi xảy ra khi thực hiện thao tác. Vui lòng thử lại sau.');
        });
    },
    submitComment: async function(event, topicId) {
        event.preventDefault();
        const content = document.getElementById(`commentContent-${topicId}`).value.trim();
        const imageInput = document.getElementById(`commentImage-${topicId}`);
        const image = imageInput.files[0];
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        if (!content) {
            alert("Vui lòng nhập nội dung bình luận.");
            return;
        }
        const formData = new FormData();
        formData.append("Content", content);
        formData.append("TopicId", topicId);
        if (image) formData.append("ImageUpload", image);
        try {
            const response = await fetch('/Comments/Create', {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });
            const result = await response.json();
            if (result.success) {
                const commentList = document.querySelector(`#comment-block-${topicId} .comments-list`);
                if (commentList) {
                    commentList.insertAdjacentHTML('afterbegin', TopicManager.createCommentElement(result.comment));
                }
                document.getElementById(`commentContent-${topicId}`).value = '';
                imageInput.value = '';
            } else {
                alert(result.message || 'Gửi bình luận thất bại.');
            }
        } catch (err) {
            console.error(err);
            alert('Lỗi gửi bình luận.');
        }
    },
    createCommentElement: function(comment) {
        return `
            <div class="comment-item d-flex mb-3" id="topic-comment-${comment.id}">
                <img src="${comment.avatar}" alt="Avatar" class="comment-avatar me-2" />
                <div class="comment-body">
                    <div class="comment-header d-flex justify-content-between">
                        <div>
                            <strong class="text-primary">
                                ${comment.username} –
                                <span class="role">${TopicManager.getRoleText(comment.role)}</span>
                            </strong>
                        </div>
                        <small class="text-muted">${comment.createdAt}</small>
                    </div>
                    <div class="comment-content">
                        ${comment.content}
                        ${comment.imageUrl ? `<div class='comment-image'><img src='${comment.imageUrl}' alt='Ảnh bình luận' /></div>` : ''}
                    </div>
                    <div class="comment-actions">
                        <button class="custom-reply-btn" data-comment-id="${comment.id}">
                            <i class="fas fa-reply"></i> Trả lời
                        </button>
                    </div>
                    <div class="reply-form mt-2" id="topic-reply-form-${comment.id}" style="display: none;">
                        <form class="topic-nested-reply-form" onsubmit="TopicManager.submitReply(event, ${comment.id})" enctype="multipart/form-data">
                            <input type="hidden" name="ParentCommentId" value="${comment.id}" />
                            <input type="hidden" name="TopicId" value="${comment.topicId}" />
                            <input type="hidden" name="__RequestVerificationToken" value="${document.querySelector('input[name=__RequestVerificationToken]')?.value || ''}" />
                            <div class="mb-2">
                                <textarea name="Content" class="form-control" placeholder="Nhập phản hồi..." rows="2" required></textarea>
                            </div>
                            <div class="mb-2">
                                <input type="file" name="ImageUpload" class="form-control" accept="image/*" />
                            </div>
                            <div class="d-flex justify-content-end">
                                <button type="button" class="btn btn-light me-2" onclick="TopicManager.toggleReplyForm('${comment.id}')">Hủy</button>
                                <button type="submit" class="btn btn-purple">Gửi phản hồi</button>
                            </div>
                        </form>
                    </div>
                    <div class="nested-replies" id="topic-replies-${comment.id}"></div>
                </div>
            </div>
        `;
    }
};

document.addEventListener('DOMContentLoaded', function() {
    // Add event delegation for reply buttons
    document.addEventListener('click', function(e) {
        if (e.target.closest('.custom-reply-btn')) {
            e.preventDefault();
            e.stopPropagation();
            const btn = e.target.closest('.custom-reply-btn');
            const commentId = btn.getAttribute('data-comment-id');
            if (commentId) {
                TopicManager.toggleReplyForm(commentId);
            }
        }
    });

    var topicIdToDelete = null;
    // Gán sự kiện cho nút mở modal xóa
    window.TopicManager.showDeleteTopicModal = function(topicId) {
        topicIdToDelete = topicId;
        const modal = new bootstrap.Modal(document.getElementById('deleteTopicModal'));
        modal.show();
    };
    // Gán sự kiện cho nút xác nhận xóa
    var confirmDeleteBtn = document.getElementById('confirmDeleteTopicBtn');
    if (confirmDeleteBtn) {
        confirmDeleteBtn.onclick = function() {
            if (topicIdToDelete) {
                fetch(`/Topics/Delete/${topicIdToDelete}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                    }
                })
                .then(response => {
                    if (response.redirected) {
                        window.location.href = response.url;
                    } else {
                        window.location.reload();
                    }
                });
            }
        };
    }
}); 