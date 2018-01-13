window.onload = function() {
  // Event bus object
  var bus = new Vue();

  Vue.component('rpa-node-instance', {
    template: '#tmpl-node-instance',

    props: ['id', 'type', 'displayType', 'name', 'prop'],

    data: function() {
      return {
        isSuccessorOn: false,
      };
    },

    computed: {
      canHaveSuccessor: function() {
        return this.type !== 'End';
      },
    },

    updated: function() {
      this.$emit('nodepropertychange', {
        id: this.id,
        name: this.name,
        prop: this.prop,
      });
    },

    methods: {
      onClick: function(e) {
        bus.$emit('node-instance.click', this);
      },

      onSuccessorDragEnter: function() {
        this.isSuccessorOn = true;
      },

      onSuccessorDragOver: function(e) {
        if (e.preventDefault) {
          e.preventDefault();
        }
        return false;
      },

      onSuccessorDragLeave: function() {
        this.isSuccessorOn = false;
      },

      onSuccessorDrop: function(e) {
        if (e.stopPropagation) {
          e.stopPropagation();
        }
        if (e.preventDefault) {
          e.preventDefault();
        }

        this.isSuccessorOn = false;

        // Get dropped node class info
        var nodeClass = JSON.parse(e.dataTransfer.getData('text'));

        // Notify to parent
        this.$emit('successordrop', this.id, nodeClass);

        return false;
      },
    },
  });

  Vue.component('rpa-node-class', {
    template: '#tmpl-node-class',

    props: ['type', 'displayType'],

    methods: {
      onDragStart: function(e) {
        // Set dragged node class info
        e.dataTransfer.setData('text', JSON.stringify({
          type: this.type,
          displayType: this.displayType,
        }));
      },
    },
  });

  Vue.component('rpa-node-palette', {
    template: '#tmpl-node-palette',

    data: function() {
      return {
        // Node classes for making workflow
        nodeClasses: [
          {type: 'ImageMatching', displayType: 'Image Matching'},
        ],
      };
    },
  });

  Vue.component('rpa-workflow-canvas', {
    template: '#tmpl-workflow-canvas',

    data: function() {
      return {
        workflow: [
          {id: uuid(), type: 'Start', displayType: 'Start', name: 'Start'},
          {id: uuid(), type: 'End', displayType: 'End', name: 'End'},
        ],
      };
    },

    methods: {
      findPositionById: function(id) {
        for (var i = 0; i < this.workflow.length; i++) {
          if (this.workflow[i].id === id) {
            return i;
          }
        }
        return -1;
      },

      onSuccessorDrop: function(predecessorId, nodeClass) {
        var nodeInstance = {
          id: uuid(),
          type: nodeClass.type,
          displayType: nodeClass.displayType,
          name: nodeClass.displayType,

          // TODO
          prop: {
            imageUrlPath: "",
            startPos: [0, 0],
            endPos: [0, 0],
            windowTitle: "",
          }
        };

        // Add to workflow
        var i = this.findPositionById(predecessorId);
        this.workflow.splice(i + 1, 0, nodeInstance);

        return false;
      },

      onNodePropertyChange: function(e) {
        for (var node of this.workflow) {
          if (node.id === e.id) {
            node.name = e.name;
            node.prop = e.prop;
            break;
          }
        }
      },

      saveWorkflow: function(callback) {
        callback = callback || function() {};

        var xhr = new XMLHttpRequest();
        xhr.onreadystatechange = function() {
          if (this.readyState === 4) {
            if (this.status === 200) {
              callback(JSON.parse(this.response));
            } else {
              callback(null, new Error("Server returned status " + this.status));
            }
          }
        };

        xhr.onerror = function(err) {
          callback(null, err);
        };

        xhr.open('POST', '/workflow/save');
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.responseType = 'application/json';
        xhr.send(JSON.stringify(this.workflow));
      },

      onSaveButtonClick: function() {
        this.saveWorkflow();
      },

      onExecuteButtonClick: function() {
        this.saveWorkflow(function(resp, err) {
          if (err) {
            alert(err);
            return;
          }

          location.href = 'openrpa:execute/' + resp.id;
        });
      },
    },
  });

  Vue.component('rpa-node-property-panel', {
    template: '#tmpl-node-property-panel',

    props: ['active'],

    data: function() {
      return {
        nodeInstance: null,
      };
    },

    computed: {
      nodeType: function() {
        return (this.nodeInstance || {}).type;
      },
    },

    methods: {
      activate: function(nodeInstance) {
        this.nodeInstance = nodeInstance;
      },
    },
  });

  Vue.component('rpa-image-matching-node-property', {
    template: '#tmpl-image-matching-node-property',

    props: ['nodeInstance'],

    computed: {
      hasImage: function() {
        return this.nodeInstance.prop.imageUrlPath !== "";
      },

      hasRect: function() {
        return this.nodeInstance.prop.startPos[0] !== 0 &&
          this.nodeInstance.prop.startPos[1] !== 0 &&
          this.nodeInstance.prop.endPos[0] !== 0 &&
          this.nodeInstance.prop.endPos[1] !== 0;
      },
    },

    methods: {
      onCaptureButtonClick: function() {
        var self = this;
        var socket = io.connect("http://localhost:5555/capture");

        // TODO: socket error handling
        socket.on('connect', function() {
          console.log('connected.');
        });

        var capturing = false;

        socket.on('receiving capture ready', function() {
          if (capturing) {
            return;
          }

          // TODO: get from cookie
          var sessionID = document.querySelector('#session-id').getAttribute('data-session-id');

          // Launch local capture application
          // (Not use location.href for Chrome/IE support)
          var iframe = document.createElement('iframe');
          iframe.style.display = 'none';
          iframe.src = 'openrpa:capture/' + sessionID;
          document.body.appendChild(iframe);

          capturing = true;
        });

        socket.on('receive capture', function(data) {
          capturing = false;

          console.log(data);
          self.nodeInstance.prop.imageUrlPath = data.path;
          self.nodeInstance.prop.windowTitle = data.title;

          socket.close();

          self.$refs.captureImageDialog.show();
        });
      },

      onCaptureImageClick: function() {
        this.$refs.captureImageDialog.show();
      },
    },
  });

  Vue.component('rpa-image-matching-capture-image-dialog', {
    template: '#tmpl-image-matching-capture-image-dialog',

    props: ['nodeInstance'],

    data: function() {
      return {
        imageUrlPath: "",
        startPos: null,
        endPos: null,
      };
    },

    methods: {
      show: function() {
        var prop = JSON.parse(JSON.stringify(this.nodeInstance.prop));
        this.imageUrlPath = prop.imageUrlPath;
        this.startPos = prop.startPos;
        this.endPos = prop.endPos;

        // Use setTimeout for propagating data change
        var self = this;
        setTimeout(function() {
          self.$refs.canvas.initialize();
          self.$refs.dialog.show();
        });
      },

      onSave: function() {
        this.nodeInstance.prop.startPos = this.startPos;
        this.nodeInstance.prop.endPos = this.endPos;
      },

      onCancel: function() {
      },
    },
  });

  Vue.component('rpa-image-matching-capture-image-dialog-canvas', {
    template: '#tmpl-image-matching-capture-image-dialog-canvas',

    props: ['imageUrlPath', 'startPos', 'endPos'],

    data: function() {
      return {
        isMouseDown: false,
      };
    },

    methods: {
      initialize: function() {
        this.isMouseDown = false;
        this.draw();
      },

      draw: function() {
        var self = this;
        var ctx = this.$refs.canvas.getContext('2d');
        var img = new Image();

        img.onload = function() {
          self.$refs.canvas.width = img.width;
          self.$refs.canvas.height = img.height;
          ctx.drawImage(img, 0, 0);

          self.drawRect(ctx);
        }
        img.src = this.imageUrlPath;
      },

      drawRect: function(ctx) {
        var startPos = this.startPos;
        var endPos = this.endPos;

        if (startPos[0] === endPos[0] &&
            startPos[1] === endPos[1]) {
          return;
        }

        // Set line style
        ctx.strokeStyle = "#00ff00";
        ctx.lineWidth = 5;
        ctx.setLineDash([2, 3]);

        ctx.beginPath();

        // Top
        ctx.moveTo(startPos[0], startPos[1]);
        ctx.lineTo(endPos[0], startPos[1]);

        // Bottom
        ctx.moveTo(startPos[0],endPos[1]);
        ctx.lineTo(endPos[0],endPos[1]);

        // Right
        ctx.moveTo(endPos[0],startPos[1]);
        ctx.lineTo(endPos[0],endPos[1]);

        // Left
        ctx.moveTo(startPos[0],startPos[1]);
        ctx.lineTo(startPos[0],endPos[1]);

        ctx.stroke();
      },

      onMouseDown: function(e) {
        this.isMouseDown = true;

        var rect = e.target.getBoundingClientRect();
        this.startPos[0] = e.clientX - rect.left;
        this.startPos[1] = e.clientY - rect.top;
      },

      onMouseMove: function(e) {
        if (!this.isMouseDown) {
          return;
        }

        var rect = e.target.getBoundingClientRect();
        this.endPos[0] = e.clientX - rect.left;
        this.endPos[1] = e.clientY - rect.top;

        this.draw();
      },

      onMouseUp: function() {
        this.isMouseDown = false;
      },
    },
  });

  new Vue({
    el: '#app',

    created: function() {
      var self = this;
      bus.$on('node-instance.click', function(nodeInstance) {
        self.activateNodePropertyPanel(nodeInstance);
      });
    },

    data: {
      isNodePropertyPanelActive: false,
    },

    methods: {
      activateNodePropertyPanel: function(nodeInstance) {
        this.isNodePropertyPanelActive = true;
        this.$refs.nodePropertyPanel.activate(nodeInstance);
      },

      onTitleClick: function() {
        location.href = '/';
      },
    },
  });
};
