window.onload = function() {
  // Event bus object
  var bus = new Vue();

  Vue.component('rpa-node-instance', {
    template: '#tmpl-node-instance',

    props: ['type', 'name', 'prop'],

    methods: {
      onClick: function(e) {
        // TODO: set json data instead of instance itself
        bus.$emit('node-instance.click', this);
      },
    },
  });

  Vue.component('rpa-node-class', {
    template: '#tmpl-node-class',

    props: ['type'],

    methods: {
      onDragStart: function(e) {
        // Set dragged node class info
        e.dataTransfer.setData('text', JSON.stringify({
          type: this.type
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
          {type: 'ImageMatching'},
        ],
      };
    },
  });

  Vue.component('rpa-workflow-canvas', {
    template: '#tmpl-workflow-canvas',

    data: function() {
      return {
        workflow: [
          {type: 'Start', name: 'Start'},
          {type: 'End', name: 'End'},
        ],
      };
    },

    methods: {
      onDragOver: function(e) {
        if (e.preventDefault) {
          e.preventDefault();
        }
        return false;
      },

      onDrop: function(e) {
        if (e.stopPropagation) {
          e.stopPropagation();
        }
        if (e.preventDefault) {
          e.preventDefault();
        }

        // Get dropped node class info
        var nodeClass = JSON.parse(e.dataTransfer.getData('text'));
        var nodeInstance = {
          type: nodeClass.type,
          name: nodeClass.type,

          // TODO
          prop: {
            imageURLPath: "",
            startPos: [0, 0],
            endPos: [0, 0],
            windowTitle: "",
          }
        };

        // Add to workflow
        this.workflow.splice(this.workflow.length - 1, 0, nodeInstance);

        return false;
      },

      onExecuteButtonClick: function() {
        // TODO: serialize workflow and send
        return;

        var xhr = new XMLHttpRequest();
        xhr.onreadystatechange = function() {
          if (this.readyState === 4 && this.status === 200) {
            var zip = new Blob([this.response], {type: 'application/zip'});

            location.href = URL.createObjectURL(zip);
          }
        };
        xhr.open('POST', '/download');
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.responseType = 'blob';
        xhr.send(JSON.stringify(workflow));
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
        return this.nodeInstance.prop.imageURLPath !== "";
      },
    },

    methods: {
      onCaptureButtonClick: function() {
        var self = this;
        var socket = io.connect("http://localhost:5555/capture");

        // TODO: socket error handling
        socket.on('connect', function() {
          console.log('connected.');

          // Listen for sending screen capture
          socket.emit('listen capture');
        });

        socket.on('ready receiving capture', function() {
          // TODO: get from cookie
          var sessionID = document.querySelector('#session-id').getAttribute('data-session-id');

          // Launch local capture application
          location.href = 'openrpa:capture/' + sessionID;
        });

        socket.on('receive capture', function(data) {
          console.log(data);
          self.nodeInstance.prop.imageURLPath = data.path;
          self.nodeInstance.prop.windowTitle = data.title;

          socket.close();
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

    methods: {
      show: function() {
        this.$refs.canvas.initialize();
        this.$refs.dialog.show();
      },

      onSave: function() {
        // TODO
      },

      onCancel: function() {
        // TODO
      },
    },
  });

  Vue.component('rpa-image-matching-capture-image-dialog-canvas', {
    template: '#tmpl-image-matching-capture-image-dialog-canvas',

    props: ['nodeInstance'],

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

        img.src = this.nodeInstance.prop.imageURLPath;
      },

      drawRect: function(ctx) {
        var startPos = this.nodeInstance.prop.startPos;
        var endPos = this.nodeInstance.prop.endPos;

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
        this.nodeInstance.prop.startPos = [e.clientX - rect.left,
                                           e.clientY - rect.top];
      },

      onMouseMove: function(e) {
        if (!this.isMouseDown) {
          return;
        }

        var rect = e.target.getBoundingClientRect();
        this.nodeInstance.prop.endPos = [e.clientX - rect.left,
                                         e.clientY - rect.top];

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
